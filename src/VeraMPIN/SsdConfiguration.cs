using System.Text.Json;

namespace VeraMPIN;

internal sealed class SsdConfiguration
{
    public List<MountProfile> Profiles { get; set; } = [];

    private static string SettingsPath => Path.Combine(AppDataPaths.CurrentDirectory, "settings.json");

    public static SsdConfiguration? Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return null;
        }

        var json = File.ReadAllText(SettingsPath);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty(nameof(Profiles), out _))
        {
            return JsonSerializer.Deserialize<SsdConfiguration>(json)
                ?? throw new InvalidDataException("The saved mount configuration could not be read.");
        }

        var legacy = JsonSerializer.Deserialize<LegacySsdConfiguration>(json)
            ?? throw new InvalidDataException("The saved mount configuration could not be read.");
        var migrated = new SsdConfiguration
        {
            Profiles =
            [
                new MountProfile
                {
                    Name = string.IsNullOrWhiteSpace(legacy.ContainerPath)
                        ? legacy.DiskName
                        : Path.GetFileName(legacy.ContainerPath),
                    SourceType = legacy.SourceType,
                    DiskUniqueId = legacy.DiskUniqueId,
                    DiskIdentity = string.IsNullOrWhiteSpace(legacy.DiskUniqueId)
                        ? ""
                        : $"UniqueId:{legacy.DiskUniqueId.Trim()}",
                    DiskName = legacy.DiskName,
                    DiskSize = legacy.DiskSize,
                    PartitionNumber = legacy.PartitionNumber,
                    PartitionSize = legacy.PartitionSize,
                    Partitions = legacy.Partitions,
                    PreferredMountLetter = legacy.MountLetter,
                    MountedLetter = legacy.MountedLetter ??
                        (legacy.MountedVolumeSerial.HasValue ? legacy.MountLetter : null),
                    ContainerPath = legacy.ContainerPath,
                    MountedVolumeSerial = legacy.MountedVolumeSerial,
                    MountedVolumeSize = legacy.MountedVolumeSize,
                    MountedVolumeLabel = legacy.MountedVolumeLabel
                }
            ]
        };
        SecretStore.MigrateLegacySecret(migrated.Profiles[0].Id);
        migrated.Save();
        return migrated;
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

}

internal sealed class LegacySsdConfiguration
{
    public string DiskUniqueId { get; set; } = "";
    public string DiskName { get; set; } = "";
    public long DiskSize { get; set; }
    public int PartitionNumber { get; set; }
    public long PartitionSize { get; set; }
    public List<PartitionSummary> Partitions { get; set; } = [];
    public char MountLetter { get; set; } = 'F';
    public char? MountedLetter { get; set; }
    public MountSourceType SourceType { get; set; }
    public string? ContainerPath { get; set; }
    public uint? MountedVolumeSerial { get; set; }
    public long? MountedVolumeSize { get; set; }
    public string? MountedVolumeLabel { get; set; }
}
