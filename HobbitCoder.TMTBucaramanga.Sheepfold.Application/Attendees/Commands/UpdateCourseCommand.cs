using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

public class UpdateCourseCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid AttendeeCourseId { get; set; }
    public CourseStatus Status { get; set; }
    public DateOnly? FinishedOn { get; set; }
}

public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public UpdateCourseCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateCourseCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var attendeeCourse = await ContactAndCourseHelpers.FindCourseAsync(_dbContext, attendee.Id, request.AttendeeCourseId, cancellationToken);

        var before = new { Course = attendeeCourse.Course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn };
        attendeeCourse.Status = request.Status;
        attendeeCourse.FinishedOn = request.Status == CourseStatus.Finished ? request.FinishedOn : null;

        _audit.Record(AuditModule.Courses, AuditActions.CourseUpdated, attendee.Id, $"{attendee.FullName} / {attendeeCourse.Course.Name}", before,
            new { Course = attendeeCourse.Course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.Status).IsInEnum().WithMessage("Status must be InProgress or Finished");
        RuleFor(command => command.FinishedOn).Null().When(command => command.Status != CourseStatus.Finished)
            .WithMessage("FinishedOn only applies to finished courses");
        RuleFor(command => command.FinishedOn)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("FinishedOn cannot be in the future");
    }
}
