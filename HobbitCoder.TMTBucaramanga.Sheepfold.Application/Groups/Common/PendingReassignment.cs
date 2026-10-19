namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class PendingReassignment
{
    public required Guid AttendeeId { get; set; }
    public required string FullName { get; set; }
    public required Guid CurrentGroupId { get; set; }
    public required string CurrentGroup { get; set; }
    public required int CurrentYear { get; set; }
    public required Guid GenerationId { get; set; }
    public required string GenerationName { get; set; }
}
