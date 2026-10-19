using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Queries;

/// <summary>
/// Actividad del asistente: sus marcas de asistencia con el grupo de ese momento, y los datos generales de los
/// seguimientos que el usuario puede consultar (nunca su contenido; el aprendiz no ve seguimientos).
/// </summary>
public class GetAttendeeActivityQuery : IRequest<AttendeeActivityResponse>
{
    public Guid Id { get; set; }
}

public class GetAttendeeActivityQueryHandler : IRequestHandler<GetAttendeeActivityQuery, AttendeeActivityResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetAttendeeActivityQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<AttendeeActivityResponse> Handle(GetAttendeeActivityQuery request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);

        var marks = await _dbContext.AttendeeAttendances
            .Where(attendance => attendance.AttendeeId == attendee.Id)
            .OrderByDescending(attendance => attendance.Event.StartsAt)
            .Select(attendance => new { attendance.EventId, attendance.Event.Title, attendance.Event.StartsAt, attendance.Mark })
            .Take(200)
            .ToListAsync(cancellationToken);

        var memberships = await _dbContext.GroupMemberships
            .IgnoreQueryFilters()
            .Where(membership => membership.AttendeeId == attendee.Id)
            .Select(membership => new { membership.StartedAt, membership.EndedAt, Name = membership.Group.Generation.Name + " · " + membership.Group.Name + " · " + membership.Group.Year })
            .ToListAsync(cancellationToken);

        var attendance = marks.Select(mark => new AttendanceHistoryItem
        {
            EventId = mark.EventId,
            EventTitle = mark.Title,
            StartsAt = mark.StartsAt,
            Mark = mark.Mark,
            GroupAtTheTime = memberships.FirstOrDefault(membership => membership.StartedAt <= mark.StartsAt && (membership.EndedAt == null || membership.EndedAt > mark.StartsAt))?.Name
        })
            .ToList();

        var followUps = new List<FollowUpGeneralItem>();

        if (_access.CanFollowUp)
        {
            var directory = await _userDirectory.GetUsersAsync(cancellationToken);
            var visible = await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken);
            var rows = await visible
                .Where(followUp => followUp.SubjectAttendeeId == attendee.Id)
                .OrderByDescending(followUp => followUp.MeetingDate ?? followUp.ScheduledDate)
                .Select(followUp => new { followUp.Id, followUp.Status, Date = followUp.MeetingDate ?? followUp.ScheduledDate, followUp.CreatedBy, followUp.IsConfidential })
                .ToListAsync(cancellationToken);

            followUps.AddRange(rows.Select(row => new FollowUpGeneralItem { Id = row.Id, Status = row.Status, Date = row.Date, AuthorName = directory.NameOf(row.CreatedBy), IsConfidential = row.IsConfidential }));
        }

        return new AttendeeActivityResponse { Attendance = attendance, FollowUps = followUps };
    }
}
