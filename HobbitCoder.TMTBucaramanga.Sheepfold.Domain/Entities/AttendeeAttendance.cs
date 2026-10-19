using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Marca de asistencia de un asistente en un evento. Clave (evento, asistente): una marca vigente por
/// persona. Sin fila = sin marca, que se interpreta como no asistió. Quién la registró queda en
/// <c>CreatedBy</c>/<c>UpdatedBy</c> y cada cambio en la auditoría.
/// </summary>
public class AttendeeAttendance : IAuditableEntity
{
    public required Guid EventId { get; set; }
    public required Guid AttendeeId { get; set; }
    public required AttendanceMark Mark { get; set; }

    public string CreatedBy { get; set; } = Guid.Empty.ToString();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual Attendee Attendee { get; set; } = null!;
}
