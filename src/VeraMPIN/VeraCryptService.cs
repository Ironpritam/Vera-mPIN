using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;
using System.Linq;

namespace VeraMPIN;

internal static class VeraCryptService
{
    private static string VeraCryptPath => Path.Combine(AppContext.BaseDirectory, "VeraCrypt-x64.exe");

    public static char FindAvailableMountLetter(char preferred)
    {
        var availableLetters = GetAvailableMountLetters().ToHashSet();
        var requested = char.ToUpperInvariant(preferred);
        if (requested is >= 'F' and <= 'Z' && availableLetters.Contains(requested))
        {
            return requested;
        }

        for (var letter = 'F'; letter <= 'Z'; letter++)
        {
            if (availableLetters.Contains(letter))
            {
                return letter;
            }
        }

        throw new InvalidOperationException("No unused drive letters remain for the VeraCrypt mount.");
    }

    public static IReadOnlyList<char> GetAvailableMountLetters()
    {
        var usedLetters = DriveInfo.GetDrives()
            .Select(drive => char.ToUpperInvariant(drive.Name[0]))
            .ToHashSet();
        return Enumerable.Range('F', 'Z' - 'F' + 1)
            .Select(value => (char)value)
            .Where(letter => !usedLetters.Contains(letter))
            .ToList();
    }

    public static Process StartMount(string devicePath, char mountLetter, string? password)
    {
        var arguments = new List<string> { "/q", "/v", devicePath, "/l", mountLetter.ToString() };
        if (password is not null)
        {
            arguments.Add("/s");
            arguments.Add("/p");
            arguments.Add(password);
        }

        return Start(arguments);
    }

    public static Process StartDismount(char mountLetter) =>
        Start(["/q", "/s", "/d", mountLetter.ToString()]);

    public static MountedVolume? GetMountedVolume(char mountLetter)
    {
        var root = $"{char.ToUpperInvariant(mountLetter)}:\\";
        var drive = DriveInfo.GetDrives().FirstOrDefault(candidate =>
            string.Equals(candidate.Name, root, StringComparison.OrdinalIgnoreCase));
        if (drive is null)
        {
            return null;
        }

        if (!drive.IsReady)
        {
            throw new InvalidOperationException($"Drive {mountLetter}: exists but is not ready; its state is unknown, so the app will not treat it as mounted or dismounted.");
        }

        var label = new StringBuilder(256);
        var fileSystem = new StringBuilder(64);
        if (!GetVolumeInformation(root, label, label.Capacity, out var serial, out _, out _, fileSystem, fileSystem.Capacity))
        {
            throw new InvalidOperationException($"Drive {mountLetter}: is present but its volume identity could not be read.");
        }

        return new MountedVolume(serial, drive.TotalSize, label.ToString());
    }

    public static async Task<MountedVolume> WaitForMountedVolumeAsync(char mountLetter, Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        DateTime? processExitTime = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var volume = GetMountedVolume(mountLetter);
                if (volume is not null)
                {
                    return volume;
                }
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            if (process.HasExited)
            {
                processExitTime ??= DateTime.UtcNow;
                if (DateTime.UtcNow - processExitTime > TimeSpan.FromSeconds(8))
                {
                    throw new InvalidOperationException($"VeraCrypt exited with code {process.ExitCode} without mounting {mountLetter}:. The requested drive letter may have been rejected; choose another available letter and retry.");
                }
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"VeraCrypt did not mount {mountLetter}: within {timeout.TotalSeconds:F0} seconds. Check any VeraCrypt prompt, then retry.");
    }

    public static async Task<bool> WaitForDismountAsync(char mountLetter, MountedVolume expectedVolume, Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        DateTime? processExitTime = null;
        while (DateTime.UtcNow < deadline)
        {
            MountedVolume? currentVolume;
            try
            {
                currentVolume = GetMountedVolume(mountLetter);
            }
            catch (InvalidOperationException)
            {
                await Task.Delay(500);
                continue;
            }

            if (currentVolume is null || currentVolume != expectedVolume)
            {
                return true;
            }

            if (process.HasExited)
            {
                processExitTime ??= DateTime.UtcNow;
                if (DateTime.UtcNow - processExitTime > TimeSpan.FromSeconds(8))
                {
                    throw new InvalidOperationException($"VeraCrypt exited with code {process.ExitCode}, but {mountLetter}: remains mounted. Close files using the drive and retry.");
                }
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"VeraCrypt did not dismount {mountLetter}: within {timeout.TotalSeconds:F0} seconds. Close files using the drive and retry; no forced dismount was attempted.");
    }

    private static Process Start(IReadOnlyList<string> arguments)
    {
        if (!File.Exists(VeraCryptPath))
        {
            throw new FileNotFoundException("VeraCrypt-x64.exe must be beside this app.", VeraCryptPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = VeraCryptPath,
            UseShellExecute = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            return Process.Start(startInfo)
                ?? throw new InvalidOperationException("Windows did not start VeraCrypt.");
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            throw new OperationCanceledException("VeraCrypt elevation was cancelled. The volume was not mounted.", exception);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformation(
        string rootPathName,
        StringBuilder volumeNameBuffer,
        int volumeNameSize,
        out uint volumeSerialNumber,
        out uint maximumComponentLength,
        out uint fileSystemFlags,
        StringBuilder fileSystemNameBuffer,
        int fileSystemNameSize);
}