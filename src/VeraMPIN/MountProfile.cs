using System.Text.Json.Serialization;

namespace VeraMPIN;

internal sealed class MountProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public MountSourceType SourceType { get; set; }
    public string DiskUniqueId { get; set; } = "";
    public string DiskName { get; set; } = "";
    public string DiskIdentity { get; set; } = "";
    public string BusType { get; set; } = "";
    public long DiskSize { get; set; }
    public int PartitionNumber { get; set; }
    public long PartitionSize { get; set; }
    public long PartitionOffset { get; set; }
    public string PartitionGuid { get; set; } = "";
    public bool IsSystemDisk { get; set; }
    public List<PartitionSummary> Partitions { get; set; } = [];
    public char PreferredMountLetter { get; set; } = 'F';
    public char? MountedLetter { get; set; }
    public string? ContainerPath { get; set; }
    public uint? MountedVolumeSerial { get; set; }
    public long? MountedVolumeSize { get; set; }
    public string? MountedVolumeLabel { get; set; }

    public void ClearMountedState()
    {
        MountedLetter = null;
        MountedVolumeSerial = null;
        MountedVolumeSize = null;
        MountedVolumeLabel = null;
    }

    [JsonIgnore]
    public bool HasMpin => SecretStore.Exists(Id);

    [JsonIgnore]
    public string DisplayName => $"{Name}{(MountedLetter is char letter ? $"  ·  Mounted as {letter}:" : "")}{(HasMpin ? "  ·  mPIN" : "")}";

    public override string ToString() => DisplayName;
}

internal enum MountSourceType
{
    Partition,
    ContainerFile
}
