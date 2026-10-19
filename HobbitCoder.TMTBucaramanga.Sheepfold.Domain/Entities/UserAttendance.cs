using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Marca de asistencia de un usuario (cuenta de Identity) en un evento. Clave (evento, usuario).</summary>
public class UserAttendance : IAuditableEntity
{
    public required Guid EventId { get; set; }
    public required Guid UserId { get; set; }
    public required AttendanceMark Mark { get; set; }

    public string CreatedBy { get; set; } = Guid.Empty.ToString();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual Event Event { get; set; } = null!;
}
