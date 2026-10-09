using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees;

/// <summary>Asistentes del alcance (§4, S05): el director ve todos; los demás, los de sus grupos.</summary>
public class GetAttendeesQuery : IRequest<PagedResult<AttendeeListItem>>
{
    public string? Search { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? GroupId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsServer { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

/// <summary>Ficha del asistente (HU-11): datos actuales, contactos, cursos y advertencias.</summary>
public class GetAttendeeQuery : IRequest<AttendeeProfileResponse>
{
    public Guid Id { get; set; }
}

/// <summary>Historial de grupos y generaciones, cada uno con su año y periodo (§8, §13).</summary>
public class GetAttendeeHistoryQuery : IRequest<List<MembershipHistoryItem>>
{
    public Guid Id { get; set; }
}

/// <summary>
/// Actividad del asistente: sus marcas de asistencia con el grupo de ese momento, y los datos generales de los
/// seguimientos que el usuario puede consultar (nunca su contenido; el aprendiz no ve seguimientos).
/// </summary>
public class GetAttendeeActivityQuery : IRequest<AttendeeActivityResponse>
{
    public Guid Id { get; set; }
}

public class AttendeeQueryHandlers :
    IRequestHandler<GetAttendeesQuery, PagedResult<AttendeeListItem>>,
    IRequestHandler<GetAttendeeQuery, AttendeeProfileResponse>,
    IRequestHandler<GetAttendeeHistoryQuery, List<MembershipHistoryItem>>,
    IRequestHandler<GetAttendeeActivityQuery, AttendeeActivityResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly TimeProvider _timeProvider;

    public AttendeeQueryHandlers(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<AttendeeListItem>> Handle(GetAttendeesQuery request, CancellationToken cancellationToken = default)
    {
        var attendees = await _access.AttendeesInScopeAsync(_dbContext.Attendees, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            attendees = attendees.Where(attendee => (attendee.FirstName + " " + attendee.LastName).ToUpper().Contains(search)
                || (attendee.PhoneNumber != null && attendee.PhoneNumber.Contains(search)));
        }

        if (request.GroupId is not null)
        {
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && membership.GroupId == request.GroupId));
        }

        if (request.GenerationId is not null)
        {
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && membership.Group.GenerationId == request.GenerationId));
        }

        if (request.IsActive is not null)
        {
            attendees = attendees.Where(attendee => attendee.IsActive == request.IsActive);
        }

        if (request.IsServer is not null)
        {
            attendees = attendees.Where(attendee => attendee.IsServer == request.IsServer);
        }

        return await attendees
            .OrderBy(attendee => attendee.FirstName).ThenBy(attendee => attendee.LastName).ThenBy(attendee => attendee.Id)
            .Select(attendee => new AttendeeListItem(
                attendee.Id,
                attendee.FirstName + " " + attendee.LastName,
                attendee.Memberships.Where(membership => membership.EndedAt == null)
                    .Select(membership => new GroupRef(membership.GroupId, membership.Group.Name, membership.Group.Year,
                        membership.Group.GenerationId, membership.Group.Generation.Name, membership.Group.Generation.Profile))
                    .FirstOrDefault(),
                attendee.IsServer,
                attendee.IsActive))
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<AttendeeProfileResponse> Handle(GetAttendeeQuery request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);

        var membership = await _dbContext.GroupMemberships
            .Include(item => item.Group).ThenInclude(group => group.Generation)
            .FirstOrDefaultAsync(item => item.AttendeeId == attendee.Id && item.EndedAt == null, cancellationToken);
        var generation = membership?.Group.Generation;

        var contacts = await _dbContext.AttendeeContacts
            .Where(contact => contact.AttendeeId == attendee.Id)
            .OrderBy(contact => contact.Kind).ThenBy(contact => contact.FullName)
            .Select(contact => new ContactResponse(contact.Id, contact.Kind, contact.FullName, contact.Relationship, contact.PhoneNumber))
            .ToListAsync(cancellationToken);

        var courses = await _dbContext.AttendeeCourses
            .Where(course => course.AttendeeId == attendee.Id)
            .OrderBy(course => course.Course.Name)
            .Select(course => new CourseResponse(course.Id, course.CourseId, course.Course.Name, course.Status, course.FinishedOn))
            .ToListAsync(cancellationToken);

        var scope = await _access.GetScopeAsync(cancellationToken);
        var managesGroup = membership is not null && scope.ManagedGroupIds.Contains(membership.GroupId);

        return new AttendeeProfileResponse(
            attendee.Id, attendee.FirstName, attendee.LastName, attendee.Gender, attendee.PhoneNumber, attendee.Instagram,
            attendee.BirthDate, attendee.AgeOn(LocalCalendar.Today(_timeProvider)), attendee.EducationalStage, attendee.IsServer, attendee.IsActive,
            membership is null ? null : ToRef(membership.Group),
            generation?.RequiredContactKind,
            GenerationWarning: generation is not null && attendee.EducationalStage is { } stage && !generation.MatchesStage(stage),
            MissingRequiredContact: generation is not null && !contacts.Any(contact => contact.Kind == generation.RequiredContactKind),
            contacts,
            courses,
            new AttendeeCapabilities(
                Edit: true,
                Transfer: scope.IsDirector || (scope.Role is UserRole.Coordinator or UserRole.Leader && managesGroup),
                FollowUp: scope.IsDirector || (_access.CanFollowUp && managesGroup),
                Retire: scope.IsDirector));
    }

    public async Task<List<MembershipHistoryItem>> Handle(GetAttendeeHistoryQuery request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);

        return await _dbContext.GroupMemberships
            .IgnoreQueryFilters()
            .Where(membership => membership.AttendeeId == attendee.Id)
            .OrderByDescending(membership => membership.StartedAt)
            .Select(membership => new MembershipHistoryItem(
                new GroupRef(membership.GroupId, membership.Group.Name, membership.Group.Year, membership.Group.GenerationId,
                    membership.Group.Generation.Name, membership.Group.Generation.Profile),
                membership.StartedAt,
                membership.EndedAt))
            .ToListAsync(cancellationToken);
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

        var attendance = marks.Select(mark => new AttendanceHistoryItem(
                mark.EventId, mark.Title, mark.StartsAt, mark.Mark,
                memberships.FirstOrDefault(membership => membership.StartedAt <= mark.StartsAt && (membership.EndedAt == null || membership.EndedAt > mark.StartsAt))?.Name))
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

            followUps.AddRange(rows.Select(row => new FollowUpGeneralItem(row.Id, row.Status, row.Date, directory.NameOf(row.CreatedBy), row.IsConfidential)));
        }

        return new AttendeeActivityResponse(attendance, followUps);
    }

    private static GroupRef ToRef(Group group) =>
        new(group.Id, group.Name, group.Year, group.GenerationId, group.Generation.Name, group.Generation.Profile);
}

public class GetAttendeesQueryValidator : AbstractValidator<GetAttendeesQuery>
{
    public GetAttendeesQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
    }
}
