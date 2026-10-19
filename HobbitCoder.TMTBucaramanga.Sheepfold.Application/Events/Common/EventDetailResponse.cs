using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class EventDetailResponse
{
    public required EventSummaryResponse Event { get; set; }
    public required string Description { get; set; }
    public required PersonRef Creator { get; set; }
    public required IReadOnlyList<PersonRef> Managers { get; set; }
    public required IReadOnlyList<FileResponse> Attachments { get; set; }
    public required IReadOnlyList<EventStatus> AllowedTransitions { get; set; }
    public required EventCapabilities Capabilities { get; set; }
}
