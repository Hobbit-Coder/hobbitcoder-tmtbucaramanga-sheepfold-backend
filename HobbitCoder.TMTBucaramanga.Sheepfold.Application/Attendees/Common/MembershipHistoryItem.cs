namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class MembershipHistoryItem
{
    public required GroupRef Group { get; set; }
    public required DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
