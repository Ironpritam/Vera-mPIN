using System.Diagnostics;
using System.Text.Json;

namespace VeraMPIN;

internal static class DiskInventory
{
    private const string PowerShellQuery = """
        $ErrorActionPreference = 'Stop'
        $disks = @(Get-Disk | ForEach-Object {
            $disk = $_
            $partitions = @(Get-Partition -DiskNumber $disk.Number | ForEach-Object {
                $partition = $_
                $volume = $null
                if ($partition.DriveLetter) {
                    $volume = Get-Volume -DriveLetter $partition.DriveLetter -ErrorAction SilentlyContinue
                }
                [pscustomobject]@{
                    Number = [int]$partition.PartitionNumber
                    DriveLetter = if ($partition.DriveLetter) { [string]$partition.DriveLetter } else { '' }
                    Size = [long]$partition.Size
                    Offset = [long]$partition.Offset
                    Guid = [string]$partition.Guid
                    Label = if ($volume) { [string]$volume.FileSystemLabel } else { '' }
                }
            })
            [pscustomobject]@{
                Number = [int]$disk.Number
                FriendlyName = [string]$disk.FriendlyName
                UniqueId = [string]$disk.UniqueId
                Guid = [string]$disk.Guid
                SerialNumber = [string]$disk.SerialNumber
                Size = [long]$disk.Size
                BusType = [string]$disk.BusType
                IsSystem = [bool]$disk.IsSystem
                IsBoot = [bool]$disk.IsBoot
                Partitions = $partitions
            }
        })
        ConvertTo-Json -InputObject $disks -Depth 5 -Compress
        """;

    public static async Task<MountProfile> FindTargetBesideLauncherAsync()
    {
        var root = Path.GetPathRoot(Application.ExecutablePath);
        if (string.IsNullOrEmpty(root) || root.Length < 2 || !char.IsLetter(root[0]))
        {
            throw new InvalidOperationException("The app must be launched from a drive-letter partition to identify its USB disk.");
        }

        var launchLetter = char.ToUpperInvariant(root[0]);
        var disks = (await ReadDisksAsync()).Where(disk => string.Equals(disk.BusType, "USB", StringComparison.OrdinalIgnoreCase)).ToList();
        var launcherMatches = disks
            .SelectMany(disk => disk.Partitions.Select(partition => (disk, partition)))
            .Where(pair => string.Equals(pair.partition.DriveLetter, launchLetter.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (launcherMatches.Count != 1)
        {
            throw new InvalidOperationException("The app's drive letter did not resolve to exactly one partition on a USB disk.");
        }

        var (launcherDisk, launcherPartition) = launcherMatches[0];
        var candidates = launcherDisk.Partitions
            .Where(partition => partition.Number != launcherPartition.Number)
            .OrderByDescending(partition => partition.Size)
            .ToList();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No other partition was found on the USB disk containing the app.");
        }

        if (candidates.Count > 1 && candidates[0].Size == candidates[1].Size)
        {
            throw new InvalidOperationException("The USB disk has multiple equally sized candidate partitions; automatic selection is unsafe.");
        }

        return CreateProfile(launcherDisk, candidates[0]);
    }

    public static async Task<List<PartitionTargetOption>> GetPartitionTargetsAsync()
    {
        var disks = await ReadDisksAsync();
        return disks
            .SelectMany(disk => disk.Partitions.Select(partition =>
            {
                var profile = CreateProfile(disk, partition);
                var drive = string.IsNullOrWhiteSpace(partition.DriveLetter) ? "No drive letter" : $"{partition.DriveLetter}:";
                var label = string.IsNullOrWhiteSpace(partition.Label) ? "No volume label" : partition.Label;
                var diskKind = disk.IsSystem || disk.IsBoot
                    ? "Windows system drive"
                    : GetFriendlyDiskKind(disk);
                var systemTag = disk.IsSystem || disk.IsBoot ? "  ·  Windows system/boot disk" : "";
                return new PartitionTargetOption(
                    $"{diskKind}  ·  {disk.FriendlyName}",
                    $"Partition {partition.Number}  ·  {FormatSize(partition.Size)}  ·  {drive}  ·  {label}{systemTag}",
                    profile);
            }))
            .ToList();
    }

    public static async Task<(int DiskNumber, string DevicePath)> ResolveConfiguredTargetAsync(MountProfile profile)
    {
        var identity = string.IsNullOrWhiteSpace(profile.DiskIdentity)
            ? $"UniqueId:{profile.DiskUniqueId.Trim()}"
            : profile.DiskIdentity;
        var matches = (await ReadDisksAsync())
            .Where(disk => string.Equals(GetDiskIdentity(disk), identity, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count != 1)
        {
            throw new InvalidOperationException(matches.Count == 0
                ? "The configured disk is not connected."
                : "More than one connected disk has the configured identity; refusing to choose.");
        }

        var disk = matches[0];
        var partitions = disk.Partitions.Where(partition =>
            partition.Number == profile.PartitionNumber &&
            partition.Size == profile.PartitionSize &&
            (profile.PartitionOffset == 0 || partition.Offset == profile.PartitionOffset) &&
            (string.IsNullOrWhiteSpace(profile.PartitionGuid) ||
             string.Equals(partition.Guid, profile.PartitionGuid, StringComparison.OrdinalIgnoreCase))).ToList();

        if (partitions.Count != 1)
        {
            throw new InvalidOperationException("The configured partition is missing or its identity changed. No mount operation was started.");
        }

        var devicePath = $@"\Device\Harddisk{disk.Number}\Partition{profile.PartitionNumber}";
        return (disk.Number, devicePath);
    }

    private static MountProfile CreateProfile(DiskInfo disk, DiskPartition partition) => new()
    {
        Name = !string.IsNullOrWhiteSpace(partition.Label)
            ? partition.Label
            : $"{GetFriendlyDiskKind(disk)} {disk.FriendlyName} - Partition {partition.Number}",
        SourceType = MountSourceType.Partition,
        DiskUniqueId = disk.UniqueId,
        DiskIdentity = GetDiskIdentity(disk),
        DiskName = disk.FriendlyName,
        BusType = disk.BusType,
        DiskSize = disk.Size,
        PartitionNumber = partition.Number,
        PartitionSize = partition.Size,
        PartitionOffset = partition.Offset,
        PartitionGuid = partition.Guid,
        IsSystemDisk = disk.IsSystem || disk.IsBoot,
        Partitions = disk.Partitions.Select(item => new PartitionSummary
        {
            Number = item.Number,
            Size = item.Size,
            DriveLetter = item.DriveLetter,
            VolumeName = item.Label
        }).ToList()
    };

    private static string GetDiskIdentity(DiskInfo disk)
    {
        if (!string.IsNullOrWhiteSpace(disk.UniqueId))
        {
            return $"UniqueId:{disk.UniqueId.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(disk.Guid))
        {
            return $"Guid:{disk.Guid.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(disk.SerialNumber))
        {
            return $"Serial:{disk.SerialNumber.Trim()}|Size:{disk.Size}";
        }

        return $"Fallback:{disk.BusType}|{disk.FriendlyName}|Size:{disk.Size}";
    }

    private static async Task<List<DiskInfo>> ReadDisksAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(PowerShellQuery);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PowerShell to inspect connected disks.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Could not inspect disks: {error.Trim()}");
        }

        using var document = JsonDocument.Parse(output);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Array => JsonSerializer.Deserialize<List<DiskInfo>>(output, options) ?? [],
            JsonValueKind.Object => [JsonSerializer.Deserialize<DiskInfo>(output, options)
                ?? throw new InvalidDataException("PowerShell returned an invalid disk inventory.")],
            _ => []
        };
    }

    private static string FormatSize(long bytes) =>
        bytes >= 1_000_000_000_000L
            ? $"{bytes / 1_000_000_000_000d:0.##} TB"
            : bytes >= 1_000_000_000L
                ? $"{bytes / 1_000_000_000d:0.##} GB"
                : $"{bytes / 1_000_000d:0.##} MB";

    private static string GetFriendlyDiskKind(DiskInfo disk) =>
        disk.BusType.ToUpperInvariant() switch
        {
            "USB" => "External / USB drive",
            "NVME" or "SATA" or "ATA" or "SAS" => "Internal drive",
            "SD" or "MMC" => "Memory card",
            "VIRTUAL" => "Virtual drive",
            _ => string.IsNullOrWhiteSpace(disk.BusType) ? "Disk drive" : $"{disk.BusType} drive"
        };

    private sealed class DiskInfo
    {
        public int Number { get; set; }
        public string FriendlyName { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public string Guid { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public long Size { get; set; }
        public string BusType { get; set; } = "";
        public bool IsSystem { get; set; }
        public bool IsBoot { get; set; }
        public List<DiskPartition> Partitions { get; set; } = [];
    }

    private sealed class DiskPartition
    {
        public int Number { get; set; }
        public string DriveLetter { get; set; } = "";
        public long Size { get; set; }
        public long Offset { get; set; }
        public string Guid { get; set; } = "";
        public string Label { get; set; } = "";
    }
}
