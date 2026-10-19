using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Calendar.Queries;

/// <summary>
/// Calendario principal: eventos, cumpleaños y seguimientos pendientes del periodo que el usuario puede
/// ver. Los filtros de tipo, generación y grupo se aplican sobre su alcance; el tipo solo filtra eventos.
/// </summary>
public class GetCalendarQuery : IRequest<CalendarResponse>
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public Guid? EventTypeId { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? GroupId { get; set; }
    public bool Events { get; set; } = true;
    public bool Birthdays { get; set; } = true;
    public bool FollowUps { get; set; } = true;
}

public class GetCalendarQueryHandler : IRequestHandler<GetCalendarQuery, CalendarResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetCalendarQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    /// <summary>
    ///     Atiende &lt;see cref="GetCalendarQuery"/&gt;.
    /// </summary>
    /// <remarks>
    ///     El aprendiz no tiene capa pastoral.
    /// </remarks>
    public async Task<CalendarResponse> Handle(GetCalendarQuery request, CancellationToken cancellationToken = default)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var events = request.Events ? await EventsAsync(request, cancellationToken) : [];
        var birthdays = request.Birthdays ? await BirthdaysAsync(request, scope, directory, cancellationToken) : [];
        var followUps = new List<CalendarFollowUpItem>();
        int? pendingWithoutDate = null;

        if (request.FollowUps && _access.CanFollowUp && request.GroupId is null && request.GenerationId is null)
        {
            var visible = (await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken))
                .Where(followUp => followUp.Status == FollowUpStatus.Pending);

            followUps = await visible
                .Where(followUp => followUp.ScheduledDate >= request.From && followUp.ScheduledDate <= request.To)
                .OrderBy(followUp => followUp.ScheduledDate).ThenBy(followUp => followUp.ScheduledTime)
                .Select(followUp => new CalendarFollowUpItem { Id = followUp.Id, Date = followUp.ScheduledDate!.Value, Time = followUp.ScheduledTime, IsConfidential = followUp.IsConfidential })
                .ToListAsync(cancellationToken);

            pendingWithoutDate = await visible.CountAsync(followUp => followUp.ScheduledDate == null, cancellationToken);
        }

        return new CalendarResponse { Events = events, Birthdays = birthdays, FollowUps = followUps, PendingWithoutDate = pendingWithoutDate };
    }

    private async Task<List<EventSummaryResponse>> EventsAsync(GetCalendarQuery request, CancellationToken cancellationToken)
    {
        var from = new DateTimeOffset(LocalCalendar.StartOfDayUtc(request.From), TimeSpan.Zero);
        var to = new DateTimeOffset(LocalCalendar.StartOfDayUtc(request.To.AddDays(1)), TimeSpan.Zero);
        var events = await _access.VisibleEventsAsync(_dbContext.Events, cancellationToken);

        return await EventQueryHelpers.Filter(events, from, to, request.EventTypeId, request.GenerationId, request.GroupId)
            .OrderBy(@event => @event.StartsAt).ThenBy(@event => @event.Title)
            .Select(EventQueryHelpers.ToSummary)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Cumpleaños del periodo: asistentes del alcance y usuarios de sus grupos (todos los usuarios ministeriales
    /// para el director). La fecha de nacimiento es fecha sin hora: no se convierte de zona horaria.
    /// </summary>
    private async Task<List<BirthdayItem>> BirthdaysAsync(
        GetCalendarQuery request, AccessScope scope, IReadOnlyDictionary<Guid, DirectoryUser> directory, CancellationToken cancellationToken)
    {
        var attendees = (await _access.AttendeesInScopeAsync(_dbContext.Attendees, cancellationToken))
            .Where(attendee => attendee.IsActive && attendee.BirthDate != null);

        if (request.GroupId is not null)
        {
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && membership.GroupId == request.GroupId));
        }

        if (request.GenerationId is not null)
        {
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && membership.Group.GenerationId == request.GenerationId));
        }

        var people = (await attendees.Select(attendee => new { attendee.Id, attendee.FirstName, attendee.LastName, attendee.BirthDate }).ToListAsync(cancellationToken))
            .Select(attendee => (PersonType.Attendee, attendee.Id, $"{attendee.FirstName} {attendee.LastName}", attendee.BirthDate!.Value))
            .ToList();

        var teamQuery = _dbContext.GroupAssignments.Where(assignment => assignment.Group.IsActive);
        if (!scope.IsDirector)
        {
            var groupIds = scope.OperationalGroupIds;
            teamQuery = teamQuery.Where(assignment => groupIds.Contains(assignment.GroupId));
        }

        if (request.GroupId is not null)
        {
            teamQuery = teamQuery.Where(assignment => assignment.GroupId == request.GroupId);
        }

        if (request.GenerationId is not null)
        {
            teamQuery = teamQuery.Where(assignment => assignment.Group.GenerationId == request.GenerationId);
        }

        var userIds = scope.IsDirector && request.GroupId is null && request.GenerationId is null
            ? directory.Values.Where(user => user.IsMinistry).Select(user => user.Id).ToHashSet()
            : (await teamQuery.Select(assignment => assignment.UserId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

        people.AddRange(userIds.Select(directory.GetValueOrDefault).OfType<DirectoryUser>()
            .Where(user => user.IsActive && user.BirthDate is not null)
            .Select(user => (PersonType.User, user.Id, user.FullName, user.BirthDate!.Value)));

        var result = new List<BirthdayItem>();

        foreach (var (type, id, name, birthDate) in people)
        {
            for (var year = request.From.Year; year <= request.To.Year; year++)
            {
                var date = LocalCalendar.BirthdayIn(birthDate, year);

                if (date >= request.From && date <= request.To)
                {
                    result.Add(new BirthdayItem { PersonType = type, PersonId = id, FullName = name, Date = date, TurnsAge = year - birthDate.Year });
                }
            }
        }

        return result.OrderBy(item => item.Date).ThenBy(item => item.FullName).ToList();
    }
}

public class GetCalendarQueryValidator : AbstractValidator<GetCalendarQuery>
{
    public GetCalendarQueryValidator()
    {
        RuleFor(query => query.To).GreaterThanOrEqualTo(query => query.From).WithMessage("To must be after From");
        RuleFor(query => query).Must(query => query.To.DayNumber - query.From.DayNumber <= 92)
            .WithMessage("The period cannot exceed 92 days");
    }
}
