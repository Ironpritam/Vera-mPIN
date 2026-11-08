namespace VeraMPIN;

internal sealed record PartitionTargetOption(string Title, string Details, MountProfile Profile)
{
    public override string ToString() => $"{Title}{Environment.NewLine}{Details}";
}
