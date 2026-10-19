namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class RenewalOverviewResponse
{
    public required int Year { get; set; }
    public required IReadOnlyList<PendingReassignment> PendingAttendees { get; set; }
}
