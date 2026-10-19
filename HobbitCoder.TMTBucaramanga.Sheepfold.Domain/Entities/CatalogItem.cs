using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Valor de una lista administrable. El nombre es único dentro de su lista. Retirarlo lo desactiva: deja
/// de ofrecerse en los formularios, pero los registros históricos que lo usan lo siguen mostrando.
/// </summary>
public abstract class CatalogItem : AuditableEntity
{
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}
