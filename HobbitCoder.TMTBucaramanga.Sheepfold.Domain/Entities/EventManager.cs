namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Encargado del evento: lo administra sin ganar acceso a otros eventos o grupos.</summary>
public class EventManager
{
    public required Guid EventId { get; set; }
    public required Guid UserId { get; set; }

    public virtual Event Event { get; set; } = null!;
}
