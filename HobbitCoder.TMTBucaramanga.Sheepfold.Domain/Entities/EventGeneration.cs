namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Generación del público de un evento (público <see cref="EventAudience.Generations"/>).</summary>
public class EventGeneration
{
    public required Guid EventId { get; set; }
    public required Guid GenerationId { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual Generation Generation { get; set; } = null!;
}
