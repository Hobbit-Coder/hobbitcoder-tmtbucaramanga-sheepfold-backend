using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps;

/// <param name="Context">Grupo actual del asistente o rol del usuario.</param>
public sealed record FollowUpSubject(PersonType Type, Guid Id, string FullName, string? Context);

/// <summary>
/// Datos generales de un seguimiento (§22): persona, autor, estado y fechas. Nunca tema, lugar ni contenido: esos
/// datos solo van en el detalle autorizado.
/// </summary>
public sealed record FollowUpListItem(
    Guid Id,
    FollowUpSubject Subject,
    Guid AuthorId,
    string AuthorName,
    FollowUpStatus Status,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    DateOnly? MeetingDate,
    bool IsConfidential,
    bool CanEdit);

public sealed record FollowUpContentResponse(
    DateOnly MeetingDate,
    string Location,
    Guid TopicId,
    string TopicName,
    string Description,
    string Commitments,
    string? Observations);

/// <param name="Content">Contenido de la reunión; <c>null</c> si está pendiente o si es confidencial y el usuario no es su autor ni director.</param>
/// <param name="ContentProtected">El contenido existe pero no se muestra (confidencial).</param>
public sealed record FollowUpDetailResponse(FollowUpListItem FollowUp, FollowUpContentResponse? Content, bool ContentProtected);

/// <summary>Información de la reunión realizada (§21). Las observaciones son opcionales.</summary>
public class FollowUpContent
{
    public DateOnly MeetingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public Guid TopicId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Commitments { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public bool IsConfidential { get; set; }
}

public class FollowUpContentValidator : AbstractValidator<FollowUpContent>
{
    public FollowUpContentValidator(TimeProvider timeProvider)
    {
        RuleFor(content => content.MeetingDate)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("The meeting date cannot be in the future.");
        RuleFor(content => content.Location).NotEmpty().WithMessage("Location is required").MaximumLength(150);
        RuleFor(content => content.TopicId).NotEmpty().WithMessage("Select a topic.");
        RuleFor(content => content.Description).NotEmpty().WithMessage("Description is required").MaximumLength(4000);
        RuleFor(content => content.Commitments).NotEmpty().WithMessage("Commitments are required").MaximumLength(4000);
        RuleFor(content => content.Observations).MaximumLength(4000);
    }
}
