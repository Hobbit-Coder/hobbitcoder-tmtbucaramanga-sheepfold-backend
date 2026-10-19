namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;

public sealed class RetiredRecordResponse
{
    public required RetiredType Type { get; set; }
    public required Guid Id { get; set; }
    public required string Label { get; set; }
    public required string State { get; set; }
    public DateTime? RetiredAt { get; set; }
    public required bool CanRestore { get; set; }
    public required bool RestoreNeedsGroup { get; set; }
}
