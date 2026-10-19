using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Pertenencia de un asistente a un grupo durante un periodo. Hay a lo sumo una vigente
/// (<see cref="EndedAt"/> nulo) por asistente; un traslado cierra la vigente y abre otra en la misma
/// transacción. Las anteriores son el historial de grupos y generaciones.
/// </summary>
public class GroupMembership : BaseEntity
{
    public required Guid AttendeeId { get; set; }
    public required Guid GroupId { get; set; }
    public required DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public virtual Attendee Attendee { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;

    public bool IsCurrent => EndedAt is null;

    /// <summary>Indica si la pertenencia estaba vigente en <paramref name="moment"/>.</summary>
    public bool WasActiveAt(DateTime moment) => StartedAt <= moment && (EndedAt is null || EndedAt > moment);
}
