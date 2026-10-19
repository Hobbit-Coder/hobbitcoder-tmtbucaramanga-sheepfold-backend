using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Hecho de la auditoría ministerial: quién, cuándo, qué módulo y acción, el registro afectado (con su
/// referencia legible de ese momento) y la información anterior y resultante. Es de solo lectura y no tiene
/// claves foráneas para sobrevivir a la eliminación definitiva de lo auditado.
/// </summary>
public class AuditEntry : BaseEntity
{
    public required DateTime OccurredAt { get; set; }
    public required string ActorUserId { get; set; }
    public required AuditModule Module { get; set; }
    public required string Action { get; set; }
    public Guid? EntityId { get; set; }
    public required string Reference { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}
