using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Persona del ministerio que no ingresa a la aplicación. Su grupo y su generación actuales son los
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

    /// <summary>Dato opcional para la advertencia de generación.</summary>
    public EducationalStage? EducationalStage { get; set; }

    /// <summary>Condición de servidor; independiente de los cursos.</summary>
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
