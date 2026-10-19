using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Evento del calendario. El creador es <see cref="AuditableEntity.CreatedBy"/>. "General o por grupos"
/// se deriva del público: <see cref="EventAudience.Groups"/> es por grupos.
/// </summary>
public class Event : SoftDeleteableEntity
{
    public required string Title { get; set; }
    public required Guid EventTypeId { get; set; }
    public required DateTime StartsAt { get; set; }
    public required DateTime EndsAt { get; set; }
    public required string Location { get; set; }
    public required string Description { get; set; }
    public required EventAudience Audience { get; set; }
    public bool RequiresAttendance { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;

    public virtual EventType EventType { get; set; } = null!;
    public virtual ICollection<EventGeneration> Generations { get; set; } = [];
    public virtual ICollection<EventGroup> Groups { get; set; } = [];
    public virtual ICollection<EventManager> Managers { get; set; } = [];
    public virtual ICollection<EventAttachment> Attachments { get; set; } = [];
    public virtual ICollection<EventComment> Comments { get; set; } = [];

    public bool IsByGroups => Audience == EventAudience.Groups;
}
