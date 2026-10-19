namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class GroupHistoryEntry
{
    public required Guid AttendeeId { get; set; }
    public required string FullName { get; set; }
    public required DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
