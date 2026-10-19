using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

/// <summary>Registra un curso del catálogo. Finalizado admite fecha opcional.</summary>
public class AddCourseCommand : IRequest<Guid>
{
    public Guid AttendeeId { get; set; }
    public Guid CourseId { get; set; }
    public CourseStatus Status { get; set; }
    public DateOnly? FinishedOn { get; set; }
}

public class AddCourseCommandHandler : IRequestHandler<AddCourseCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public AddCourseCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    /// <summary>
    ///     Atiende &lt;see cref="AddCourseCommand"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Un curso figura una sola vez en la formación del asistente: su avance se actualiza en ese registro.
    /// </remarks>
    public async Task<Guid> Handle(AddCourseCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var course = await _dbContext.Courses.FirstOrDefaultAsync(item => item.Id == request.CourseId && item.IsActive, cancellationToken)
            ?? throw ValidationException.For(nameof(request.CourseId), "Select a course from the catalog.");

        if (await _dbContext.AttendeeCourses.AnyAsync(item => item.AttendeeId == attendee.Id && item.CourseId == course.Id, cancellationToken))
        {
            throw new ConflictException($"The attendee already has the course {course.Name}. Update its status instead.");
        }

        var attendeeCourse = new AttendeeCourse
        {
            AttendeeId = attendee.Id,
            CourseId = course.Id,
            Status = request.Status,
            FinishedOn = request.Status == CourseStatus.Finished ? request.FinishedOn : null
        };

        _dbContext.AttendeeCourses.Add(attendeeCourse);
        _audit.Record(AuditModule.Courses, AuditActions.CourseAdded, attendee.Id, $"{attendee.FullName} / {course.Name}", null,
            new { Course = course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return attendeeCourse.Id;
    }
}

/// <summary>Solo "En curso" y "Finalizado"; la fecha solo acompaña a un curso finalizado.</summary>
public class AddCourseCommandValidator : AbstractValidator<AddCourseCommand>
{
    public AddCourseCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.CourseId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum().WithMessage("Status must be InProgress or Finished");
        RuleFor(command => command.FinishedOn).Null().When(command => command.Status != CourseStatus.Finished)
            .WithMessage("FinishedOn only applies to finished courses");
        RuleFor(command => command.FinishedOn)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("FinishedOn cannot be in the future");
    }
}
