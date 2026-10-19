using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Grupo de un año y una generación. La renovación anual crea grupos nuevos y deja inactivos los
/// anteriores, que conservan su historia. Se identifica por año, generación y nombre.
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
