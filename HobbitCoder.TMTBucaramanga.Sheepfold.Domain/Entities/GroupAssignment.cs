using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

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
