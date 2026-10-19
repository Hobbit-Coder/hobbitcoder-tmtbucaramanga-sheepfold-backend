using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class EventSummaryResponse
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required NamedRef EventType { get; set; }
    public required DateTime StartsAt { get; set; }
    public required DateTime EndsAt { get; set; }
    public required string Location { get; set; }
    public required EventAudience Audience { get; set; }
    public required IReadOnlyList<NamedRef> Generations { get; set; }
    public required IReadOnlyList<AudienceGroupRef> Groups { get; set; }
    public required EventStatus Status { get; set; }
    public required bool RequiresAttendance { get; set; }
}
