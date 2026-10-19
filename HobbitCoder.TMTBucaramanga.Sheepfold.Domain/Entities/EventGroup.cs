namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Grupo del público de un evento (público <see cref="EventAudience.Groups"/>).</summary>
public class EventGroup
{
    public required Guid EventId { get; set; }
    public required Guid GroupId { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}
