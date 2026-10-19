using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Commands;

public class RemoveCourseCommand : IRequest
{
    public Guid AttendeeId { get; set; }
    public Guid AttendeeCourseId { get; set; }
}

public class RemoveCourseCommandHandler : IRequestHandler<RemoveCourseCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public RemoveCourseCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(RemoveCourseCommand request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.AttendeeId, cancellationToken);
        var attendeeCourse = await ContactAndCourseHelpers.FindCourseAsync(_dbContext, attendee.Id, request.AttendeeCourseId, cancellationToken);

        _dbContext.AttendeeCourses.Remove(attendeeCourse);
        _audit.Record(AuditModule.Courses, AuditActions.CourseRemoved, attendee.Id, $"{attendee.FullName} / {attendeeCourse.Course.Name}",
            new { Course = attendeeCourse.Course.Name, attendeeCourse.Status, attendeeCourse.FinishedOn }, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
