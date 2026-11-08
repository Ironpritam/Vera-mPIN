namespace VeraMPIN;

internal sealed class PartitionSummary
{
    public int Number { get; set; }
    public long Size { get; set; }
    public string DriveLetter { get; set; } = "";
    public string VolumeName { get; set; } = "";
}