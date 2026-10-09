using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Persona del ministerio que no ingresa a la aplicación (§3.2, §7). Su grupo y su generación actuales son los
/// de su pertenencia vigente: no se guardan dos veces.
/// </summary>
public class Attendee : SoftDeleteableEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }

    /// <summary>Dato opcional para la advertencia de generación (decisión P03).</summary>
    public EducationalStage? EducationalStage { get; set; }

    /// <summary>Condición de servidor (§3.2, §10); independiente de los cursos.</summary>
    public bool IsServer { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<GroupMembership> Memberships { get; set; } = [];
    public virtual ICollection<AttendeeContact> Contacts { get; set; } = [];
    public virtual ICollection<AttendeeCourse> Courses { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";

    public int? AgeOn(DateOnly today)
    {
        if (BirthDate is not { } birthDate)
        {
            return null;
        }

        var age = today.Year - birthDate.Year;
        return birthDate.AddYears(age) > today ? age - 1 : age;
    }
}

/// <summary>Tutor legal o contacto de emergencia (§9). Pueden ser varios por asistente.</summary>
public class AttendeeContact : AuditableEntity
{
    public required Guid AttendeeId { get; set; }
    public required ContactKind Kind { get; set; }
    public required string FullName { get; set; }
    public required string Relationship { get; set; }
    public required string PhoneNumber { get; set; }

    public virtual Attendee Attendee { get; set; } = null!;
}

/// <summary>
/// Curso realizado o en curso por un asistente (§10). La fuente no limita repetir un curso, así que cada
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
