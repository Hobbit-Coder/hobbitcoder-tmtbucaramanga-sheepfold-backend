using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Tutor legal o contacto de emergencia. Pueden ser varios por asistente.</summary>
public class AttendeeContact : AuditableEntity
{
    public required Guid AttendeeId { get; set; }
    public required ContactKind Kind { get; set; }
    public required string FullName { get; set; }
    public required string Relationship { get; set; }
    public required string PhoneNumber { get; set; }

    public virtual Attendee Attendee { get; set; } = null!;
}
