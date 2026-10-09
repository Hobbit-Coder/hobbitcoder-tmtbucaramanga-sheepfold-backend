using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Grupo de un año y una generación (§11). La renovación anual crea grupos nuevos y deja inactivos los
/// anteriores, que conservan su historia (§12). Se identifica por año, generación y nombre.
/// </summary>
public class Group : SoftDeleteableEntity
{
    public required int Year { get; set; }
    public required Guid GenerationId { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Generation Generation { get; set; } = null!;
    public virtual ICollection<GroupAssignment> Assignments { get; set; } = [];
    public virtual ICollection<GroupMembership> Memberships { get; set; } = [];
}

/// <summary>
/// Responsabilidad de un usuario (cuenta de Identity) en un grupo: coordinador, líder o aprendiz. Es la tabla
/// intermedia explícita de la relación N:M entre grupos y usuarios. Un coordinador puede además ser líder.
/// </summary>
public class GroupAssignment
{
    public required Guid GroupId { get; set; }

    /// <summary>Id de la cuenta en Identity (claim <c>sub</c>); no hay clave foránea entre bases.</summary>
    public required Guid UserId { get; set; }

    public required GroupResponsibility Responsibility { get; set; }

    public virtual Group Group { get; set; } = null!;
}

/// <summary>
/// Pertenencia de un asistente a un grupo durante un periodo (§13). Hay a lo sumo una vigente
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

    /// <summary>Indica si la pertenencia estaba vigente en <paramref name="moment"/> (decisión P06).</summary>
    public bool WasActiveAt(DateTime moment) => StartedAt <= moment && (EndedAt is null || EndedAt > moment);
}
