using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed record GroupRef(Guid Id, string Name, int Year, Guid GenerationId, string GenerationName, GenerationProfile GenerationProfile);

public sealed record AttendeeListItem(Guid Id, string FullName, GroupRef? Group, bool IsServer, bool IsActive);

public sealed record ContactResponse(Guid Id, ContactKind Kind, string FullName, string Relationship, string PhoneNumber);

public sealed record CourseResponse(Guid Id, Guid CourseId, string CourseName, CourseStatus Status, DateOnly? FinishedOn);

/// <param name="GenerationWarning">P03: la etapa educativa registrada no corresponde a la generación. No bloquea.</param>
/// <param name="MissingRequiredContact">§9: falta el contacto exigido por la generación actual.</param>
public sealed record AttendeeProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    Gender? Gender,
    string? PhoneNumber,
    string? Instagram,
    DateOnly? BirthDate,
    int? Age,
    EducationalStage? EducationalStage,
    bool IsServer,
    bool IsActive,
    GroupRef? Group,
    ContactKind? RequiredContactKind,
    bool GenerationWarning,
    bool MissingRequiredContact,
    IReadOnlyList<ContactResponse> Contacts,
    IReadOnlyList<CourseResponse> Courses,
    AttendeeCapabilities Capabilities);

public sealed record AttendeeCapabilities(bool Edit, bool Transfer, bool FollowUp, bool Retire);

public sealed record MembershipHistoryItem(GroupRef Group, DateTime StartedAt, DateTime? EndedAt);

/// <param name="GroupAtTheTime">Grupo al que pertenecía en la fecha del evento (§13), no el actual.</param>
public sealed record AttendanceHistoryItem(Guid EventId, string EventTitle, DateTime StartsAt, AttendanceMark Mark, string? GroupAtTheTime);

/// <summary>Datos generales de un seguimiento: nunca su contenido (§22).</summary>
public sealed record FollowUpGeneralItem(Guid Id, FollowUpStatus Status, DateOnly? Date, string AuthorName, bool IsConfidential);

public sealed record AttendeeActivityResponse(IReadOnlyList<AttendanceHistoryItem> Attendance, IReadOnlyList<FollowUpGeneralItem> FollowUps);

/// <summary>Datos personales editables de la ficha (§7). Solo nombres y apellidos son obligatorios.</summary>
public interface IAttendeeData
{
    string FirstName { get; }
    string LastName { get; }
    Gender? Gender { get; }
    string? PhoneNumber { get; }
    string? Instagram { get; }
    DateOnly? BirthDate { get; }
    EducationalStage? EducationalStage { get; }
    bool IsServer { get; }
}

public sealed class ContactData
{
    public ContactKind Kind { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class AttendeeDataValidator : AbstractValidator<IAttendeeData>
{
    public AttendeeDataValidator(TimeProvider timeProvider)
    {
        RuleFor(data => data.FirstName).NotEmpty().WithMessage("FirstName is required").MaximumLength(100);
        RuleFor(data => data.LastName).NotEmpty().WithMessage("LastName is required").MaximumLength(100);
        RuleFor(data => data.Gender).IsInEnum();
        RuleFor(data => data.PhoneNumber).MaximumLength(30);
        RuleFor(data => data.Instagram).MaximumLength(60);
        RuleFor(data => data.EducationalStage).IsInEnum();
        RuleFor(data => data.BirthDate)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("BirthDate cannot be in the future");
    }
}

/// <summary>§9: nombre completo, parentesco o relación y teléfono; un contacto incompleto no cuenta.</summary>
public class ContactDataValidator : AbstractValidator<ContactData>
{
    public ContactDataValidator()
    {
        RuleFor(contact => contact.Kind).IsInEnum();
        RuleFor(contact => contact.FullName).NotEmpty().WithMessage("FullName is required").MaximumLength(150);
        RuleFor(contact => contact.Relationship).NotEmpty().WithMessage("Relationship is required").MaximumLength(60);
        RuleFor(contact => contact.PhoneNumber).NotEmpty().WithMessage("PhoneNumber is required").MaximumLength(30);
    }
}
