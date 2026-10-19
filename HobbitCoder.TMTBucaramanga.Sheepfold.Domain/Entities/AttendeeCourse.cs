using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Curso realizado o en curso por un asistente. La fuente no limita repetir un curso, así que cada
/// registro tiene su propia clave. La fecha de finalización solo existe en cursos finalizados.
/// </summary>
public class AttendeeCourse : AuditableEntity
{
    public required Guid AttendeeId { get; set; }
    public required Guid CourseId { get; set; }
    public required CourseStatus Status { get; set; }
    public DateOnly? FinishedOn { get; set; }

    public virtual Attendee Attendee { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;
}
