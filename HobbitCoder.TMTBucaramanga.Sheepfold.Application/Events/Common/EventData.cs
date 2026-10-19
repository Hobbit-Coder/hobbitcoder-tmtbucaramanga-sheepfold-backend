using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

/// <summary>Datos del evento. El creador y el estado no se capturan: los define el servidor.</summary>
public abstract class EventData
{
    public required string Title { get; set; }
    public Guid EventTypeId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public required string Location { get; set; }
    public required string Description { get; set; }
    public EventAudience Audience { get; set; }
    public List<Guid> GenerationIds { get; set; } = [];
    public List<Guid> GroupIds { get; set; } = [];
    public bool RequiresAttendance { get; set; }
    public List<Guid> ManagerIds { get; set; } = [];
}
