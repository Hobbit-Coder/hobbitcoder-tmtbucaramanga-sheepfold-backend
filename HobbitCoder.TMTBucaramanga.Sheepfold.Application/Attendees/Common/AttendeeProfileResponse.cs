using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class AttendeeProfileResponse
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public int? Age { get; set; }
    public EducationalStage? EducationalStage { get; set; }
    public required bool IsServer { get; set; }
    public required bool IsActive { get; set; }
    public GroupRef? Group { get; set; }
    public ContactKind? RequiredContactKind { get; set; }

    /// <summary>La etapa educativa registrada no corresponde a la generación. No bloquea.</summary>
    public required bool GenerationWarning { get; set; }

    /// <summary>Falta el contacto exigido por la generación actual.</summary>
    public required bool MissingRequiredContact { get; set; }
    public required IReadOnlyList<ContactResponse> Contacts { get; set; }
    public required IReadOnlyList<CourseResponse> Courses { get; set; }
    public required AttendeeCapabilities Capabilities { get; set; }
}
