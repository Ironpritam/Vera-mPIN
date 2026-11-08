namespace VeraMPIN;

internal sealed record MountedDriveOption(char Letter, string Label, long Size, string DriveType)
{
    public override string ToString() =>
        $"{Letter}:  ·  {Label}  ·  {FormatSize(Size)}  ·  {FriendlyDriveType(DriveType)}";

    private static string FriendlyDriveType(string driveType) =>
        driveType switch
        {
            "Fixed" => "Internal or connected drive",
            "Removable" => "Removable drive",
            "Network" => "Network drive",
            "CDRom" => "CD/DVD drive",
            "Ram" => "Memory drive",
            _ => "Drive"
        };

    private static string FormatSize(long bytes) =>
        bytes >= 1_000_000_000_000L
            ? $"{bytes / 1_000_000_000_000d:0.##} TB"
            : bytes >= 1_000_000_000L
                ? $"{bytes / 1_000_000_000d:0.##} GB"
                : $"{bytes / 1_000_000d:0.##} MB";
}
