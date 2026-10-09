using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports;

/// <summary>
/// Reporte de una familia para el periodo (§24, HU-25, HU-26), limitado al alcance del usuario: el director, el ministerio
/// completo; coordinadores y líderes sus grupos; el aprendiz la información operativa de su grupo (sin
/// seguimientos). El administrador no tiene reportes ministeriales. Los registros se interpretan con el grupo de su
/// momento: un traslado no mueve asistencias anteriores.
/// </summary>
public class GetReportQuery : IRequest<ReportResponse>
{
    public ReportFamily Family { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? GroupId { get; set; }
}

public class GetReportQueryHandler : IRequestHandler<GetReportQuery, ReportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly TimeProvider _timeProvider;

    public GetReportQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _timeProvider = timeProvider;
    }

    public async Task<ReportResponse> Handle(GetReportQuery request, CancellationToken cancellationToken = default)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);
        var settings = await _dbContext.OperationalSettings.AsNoTracking().FirstAsync(cancellationToken);

        if (request.Family == ReportFamily.FollowUps && !_access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }

        var memberships = ScopedMemberships(scope, request);

        return request.Family switch
        {
            ReportFamily.Attendance => await AttendanceAsync(request, memberships, settings, cancellationToken),
            ReportFamily.People => await PeopleAsync(request, memberships, cancellationToken),
            ReportFamily.FollowUps => await FollowUpsAsync(request, cancellationToken),
            ReportFamily.Courses => await CoursesAsync(request, memberships, cancellationToken),
            ReportFamily.Events => await EventsAsync(request, cancellationToken),
            _ => await BirthdaysAsync(request, memberships, settings, cancellationToken)
        };
    }

    /// <summary>Pertenencias de asistentes del alcance, filtradas por grupo y generación.</summary>
    private IQueryable<GroupMembership> ScopedMemberships(AccessScope scope, GetReportQuery request)
    {
        var groupIds = scope.OperationalGroupIds;
        var memberships = _dbContext.GroupMemberships.Where(membership => !membership.Attendee.IsDeleted);

        if (!scope.IsDirector)
        {
            memberships = memberships.Where(membership => groupIds.Contains(membership.GroupId));
        }

        if (request.GroupId is not null)
        {
            memberships = memberships.Where(membership => membership.GroupId == request.GroupId);
        }

        if (request.GenerationId is not null)
        {
            memberships = memberships.Where(membership => membership.Group.GenerationId == request.GenerationId);
        }

        return memberships;
    }

    /// <summary>
    /// Asistencia y puntualidad. Participación = Asistió + Llegó tarde; sin marca se interpreta como no asistió.
    /// Baja asistencia (P04): participaciones / eventos del público con asistencia en la ventana &lt; umbral %.
    /// Tardanzas frecuentes (P04): "Llegó tarde" en la ventana ≥ umbral.
    /// </summary>
    private async Task<ReportResponse> AttendanceAsync(GetReportQuery request, IQueryable<GroupMembership> memberships, OperationalSettings settings, CancellationToken cancellationToken)
    {
        var lowWindowStart = settings.LowAttendanceWindowDays is { } lowDays ? request.To.AddDays(1 - lowDays) : request.From;
        var lateWindowStart = settings.FrequentLatenessWindowDays is { } lateDays ? request.To.AddDays(1 - lateDays) : request.From;
        var loadFrom = new[] { request.From, lowWindowStart, lateWindowStart }.Min();
        var fromUtc = LocalCalendar.StartOfDayUtc(loadFrom);
        var toUtc = LocalCalendar.StartOfDayUtc(request.To.AddDays(1));

        var visible = await _access.VisibleEventsAsync(_dbContext.Events, cancellationToken);
        var events = await visible
            .Where(@event => @event.RequiresAttendance && (@event.Status == EventStatus.Published || @event.Status == EventStatus.Finished)
                && @event.StartsAt >= fromUtc && @event.StartsAt < toUtc)
            .Select(@event => new EventRow(@event.Id, @event.StartsAt, @event.Audience,
                @event.Generations.Select(item => item.GenerationId).ToList(), @event.Groups.Select(item => item.GroupId).ToList()))
            .ToListAsync(cancellationToken);

        var rows = await memberships
            .Where(membership => membership.StartedAt < toUtc && (membership.EndedAt == null || membership.EndedAt > fromUtc))
            .Select(membership => new MembershipRow(membership.AttendeeId, membership.Attendee.FirstName + " " + membership.Attendee.LastName,
                membership.Attendee.IsServer, membership.Attendee.IsActive, membership.GroupId, membership.Group.GenerationId,
                membership.StartedAt, membership.EndedAt))
            .ToListAsync(cancellationToken);

        var eventIds = events.Select(@event => @event.Id).ToList();
        var marks = await _dbContext.AttendeeAttendances.Where(item => eventIds.Contains(item.EventId))
            .ToDictionaryAsync(item => (item.EventId, item.AttendeeId), item => item.Mark, cancellationToken);

        // Cada asistente del alcance en cada evento: elegible por el público con su grupo de ese momento (P06) o ya marcado.
        var facts = (from @event in events
                     from membership in rows
                     where membership.StartedAt <= @event.StartsAt && (membership.EndedAt == null || membership.EndedAt > @event.StartsAt)
                     let mark = marks.TryGetValue((@event.Id, membership.AttendeeId), out var value) ? value : (AttendanceMark?)null
                     where mark is not null || Eligible(@event, membership)
                     select new Fact(LocalCalendar.LocalDateOf(@event.StartsAt), membership.AttendeeId, membership.Name, mark))
            .ToList();

        var inPeriod = facts.Where(fact => fact.Date >= request.From && fact.Date <= request.To).ToList();
        var weeks = Weeks(request.From, request.To);
        var table = new ReportTable(["attended", "late", "participations"], weeks.Select((week, index) =>
        {
            var weekFacts = inPeriod.Where(fact => fact.Date >= week.Start && fact.Date <= week.End).ToList();
            var attended = weekFacts.Count(fact => fact.Mark == AttendanceMark.Attended);
            var late = weekFacts.Count(fact => fact.Mark == AttendanceMark.Late);
            return new ReportRow($"Semana {index + 1} · {week.Start:dd/MM}", [attended, late, attended + late]);
        }).ToList());

        var servers = rows.Where(row => row.EndedAt == null && row.IsActive && row.IsServer).Select(row => row.AttendeeId).Distinct().Count();

        return new ReportResponse(ReportFamily.Attendance, request.From, request.To,
            [
                new ReportMetric("participations", inPeriod.Count(fact => fact.Mark is not null)),
                new ReportMetric("late", inPeriod.Count(fact => fact.Mark == AttendanceMark.Late)),
                new ReportMetric("unmarked", inPeriod.Count(fact => fact.Mark is null)),
                new ReportMetric("servers", servers),
                new ReportMetric("events", events.Count(@event => LocalCalendar.LocalDateOf(@event.StartsAt) >= request.From))
            ],
            table,
            [LowAttendance(facts, lowWindowStart, request.To, settings), FrequentLateness(facts, lateWindowStart, request.To, settings)]);
    }

    private static ReportSignal LowAttendance(List<Fact> facts, DateOnly windowStart, DateOnly to, OperationalSettings settings)
    {
        if (settings.LowAttendanceThresholdPercent is not { } threshold || settings.LowAttendanceWindowDays is not { } days)
        {
            return new ReportSignal("lowAttendance", false, null, []);
        }

        var items = facts.Where(fact => fact.Date >= windowStart && fact.Date <= to)
            .GroupBy(fact => (fact.AttendeeId, fact.Name))
            .Select(group => (group.Key, Total: group.Count(), Participations: group.Count(fact => fact.Mark is not null)))
            .Where(item => item.Total > 0 && item.Participations * 100 < threshold * item.Total)
            .OrderBy(item => (double)item.Participations / item.Total)
            .Select(item => new SignalItem(item.Key.AttendeeId, item.Key.Name,
                $"{item.Participations} de {item.Total} eventos ({item.Participations * 100 / item.Total} %)"))
            .ToList();

        return new ReportSignal("lowAttendance", true, $"Participación menor al {threshold} % en los últimos {days} días", items);
    }

    private static ReportSignal FrequentLateness(List<Fact> facts, DateOnly windowStart, DateOnly to, OperationalSettings settings)
    {
        if (settings.FrequentLatenessThreshold is not { } threshold || settings.FrequentLatenessWindowDays is not { } days)
        {
            return new ReportSignal("frequentLateness", false, null, []);
        }

        var items = facts.Where(fact => fact.Date >= windowStart && fact.Date <= to && fact.Mark == AttendanceMark.Late)
            .GroupBy(fact => (fact.AttendeeId, fact.Name))
            .Where(group => group.Count() >= threshold)
            .OrderByDescending(group => group.Count())
            .Select(group => new SignalItem(group.Key.AttendeeId, group.Key.Name, $"{group.Count()} llegadas tarde"))
            .ToList();

        return new ReportSignal("frequentLateness", true, $"{threshold} o más llegadas tarde en los últimos {days} días", items);
    }

    /// <summary>Personas y organización: asistentes y servidores por grupo y generación, y cambios de grupo o generación.</summary>
    private async Task<ReportResponse> PeopleAsync(GetReportQuery request, IQueryable<GroupMembership> memberships, CancellationToken cancellationToken)
    {
        var current = await memberships
            .Where(membership => membership.EndedAt == null && membership.Attendee.IsActive)
            .Select(membership => new
            {
                membership.AttendeeId,
                membership.Attendee.IsServer,
                Label = membership.Group.Generation.Name + " · " + membership.Group.Name + " · " + membership.Group.Year,
                membership.Group.Generation.SortOrder
            })
            .ToListAsync(cancellationToken);

        var fromUtc = LocalCalendar.StartOfDayUtc(request.From);
        var toUtc = LocalCalendar.StartOfDayUtc(request.To.AddDays(1));
        var arrivals = await memberships
            .Where(membership => membership.StartedAt >= fromUtc && membership.StartedAt < toUtc)
            .Select(membership => new
            {
                membership.AttendeeId,
                membership.StartedAt,
                membership.Group.GenerationId,
                Previous = _dbContext.GroupMemberships
                    .Where(previous => previous.AttendeeId == membership.AttendeeId && previous.EndedAt == membership.StartedAt)
                    .Select(previous => (Guid?)previous.Group.GenerationId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var transfers = arrivals.Where(arrival => arrival.Previous is not null).ToList();

        var table = new ReportTable(["attendees", "servers"], current
            .GroupBy(item => (item.Label, item.SortOrder))
            .OrderBy(group => group.Key.SortOrder).ThenBy(group => group.Key.Label)
            .Select(group => new ReportRow(group.Key.Label, [group.Count(), group.Count(item => item.IsServer)]))
            .ToList());

        return new ReportResponse(ReportFamily.People, request.From, request.To,
            [
                new ReportMetric("attendees", current.Count),
                new ReportMetric("servers", current.Count(item => item.IsServer)),
                new ReportMetric("groups", table.Rows.Count),
                new ReportMetric("groupChanges", transfers.Count),
                new ReportMetric("generationChanges", transfers.Count(transfer => transfer.Previous != transfer.GenerationId))
            ],
            table, []);
    }

    /// <summary>Seguimientos realizados en el periodo y pendientes, solo del alcance pastoral (sin contenido).</summary>
    private async Task<ReportResponse> FollowUpsAsync(GetReportQuery request, CancellationToken cancellationToken)
    {
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var followUps = await _access.VisibleFollowUpsAsync(_dbContext.FollowUps, directory, cancellationToken);

        if (request.GroupId is not null)
        {
            followUps = followUps.InGroup(request.GroupId.Value);
        }

        if (request.GenerationId is not null)
        {
            followUps = followUps.InGeneration(request.GenerationId.Value);
        }

        var rows = await followUps
            .Select(followUp => new { followUp.CreatedBy, followUp.Status, followUp.MeetingDate, followUp.ScheduledDate })
            .ToListAsync(cancellationToken);

        var done = rows.Where(row => row.Status == FollowUpStatus.Done && row.MeetingDate >= request.From && row.MeetingDate <= request.To).ToList();
        var pending = rows.Where(row => row.Status == FollowUpStatus.Pending).ToList();

        var table = new ReportTable(["done", "pending"], rows
            .GroupBy(row => row.CreatedBy)
            .Select(group => new ReportRow(directory.NameOf(group.Key),
                [done.Count(row => row.CreatedBy == group.Key), pending.Count(row => row.CreatedBy == group.Key)]))
            .Where(row => row.Values.Any(value => value > 0))
            .OrderBy(row => row.Label)
            .ToList());

        return new ReportResponse(ReportFamily.FollowUps, request.From, request.To,
            [
                new ReportMetric("done", done.Count),
                new ReportMetric("pending", pending.Count),
                new ReportMetric("pendingWithoutDate", pending.Count(row => row.ScheduledDate is null))
            ],
            table, []);
    }

    /// <summary>Formación: cursos en curso y finalizados de los asistentes del alcance.</summary>
    private async Task<ReportResponse> CoursesAsync(GetReportQuery request, IQueryable<GroupMembership> memberships, CancellationToken cancellationToken)
    {
        var attendeeIds = memberships.Where(membership => membership.EndedAt == null).Select(membership => membership.AttendeeId);
        var courses = await _dbContext.AttendeeCourses
            .Where(course => attendeeIds.Contains(course.AttendeeId))
            .Select(course => new
            {
                course.AttendeeId,
                Name = course.Attendee.FirstName + " " + course.Attendee.LastName,
                Course = course.Course.Name,
                course.Status,
                course.FinishedOn
            })
            .ToListAsync(cancellationToken);

        var table = new ReportTable(["inProgress", "finished", "finishedInPeriod"], courses
            .GroupBy(course => course.Course)
            .OrderBy(group => group.Key)
            .Select(group => new ReportRow(group.Key,
            [
                group.Count(course => course.Status == CourseStatus.InProgress),
                group.Count(course => course.Status == CourseStatus.Finished),
                group.Count(course => course.FinishedOn >= request.From && course.FinishedOn <= request.To)
            ]))
            .ToList());

        var inProgress = courses.Where(course => course.Status == CourseStatus.InProgress)
            .OrderBy(course => course.Name)
            .Select(course => new SignalItem(course.AttendeeId, course.Name, course.Course))
            .ToList();

        return new ReportResponse(ReportFamily.Courses, request.From, request.To,
            [
                new ReportMetric("inProgress", inProgress.Count),
                new ReportMetric("finished", courses.Count(course => course.Status == CourseStatus.Finished))
            ],
            table, [new ReportSignal("coursesInProgress", true, null, inProgress)]);
    }

    /// <summary>
    /// Actividad y cumplimiento de eventos: cuántos se publicaron, finalizaron o cancelaron y cuántos de los que
    /// requieren asistencia tienen al menos una marca registrada.
    /// </summary>
    private async Task<ReportResponse> EventsAsync(GetReportQuery request, CancellationToken cancellationToken)
    {
        var fromUtc = LocalCalendar.StartOfDayUtc(request.From);
        var toUtc = LocalCalendar.StartOfDayUtc(request.To.AddDays(1));
        var visible = await _access.VisibleEventsAsync(_dbContext.Events, cancellationToken);

        var events = await Events.EventQueryHandlers.Filter(visible, null, null, null, request.GenerationId, request.GroupId)
            .Where(@event => @event.StartsAt >= fromUtc && @event.StartsAt < toUtc)
            .Select(@event => new
            {
                Type = @event.EventType.Name,
                @event.Status,
                @event.RequiresAttendance,
                Recorded = _dbContext.AttendeeAttendances.Any(item => item.EventId == @event.Id) || _dbContext.UserAttendances.Any(item => item.EventId == @event.Id)
            })
            .ToListAsync(cancellationToken);

        var table = new ReportTable(["total", "finished", "cancelled", "attendanceRecorded"], events
            .GroupBy(@event => @event.Type)
            .OrderBy(group => group.Key)
            .Select(group => new ReportRow(group.Key,
            [
                group.Count(),
                group.Count(@event => @event.Status == EventStatus.Finished),
                group.Count(@event => @event.Status == EventStatus.Cancelled),
                group.Count(@event => @event.RequiresAttendance && @event.Recorded)
            ]))
            .ToList());

        return new ReportResponse(ReportFamily.Events, request.From, request.To,
            [
                new ReportMetric("total", events.Count),
                new ReportMetric("published", events.Count(@event => @event.Status == EventStatus.Published)),
                new ReportMetric("finished", events.Count(@event => @event.Status == EventStatus.Finished)),
                new ReportMetric("cancelled", events.Count(@event => @event.Status == EventStatus.Cancelled)),
                new ReportMetric("attendanceRequired", events.Count(@event => @event.RequiresAttendance && @event.Status != EventStatus.Cancelled)),
                new ReportMetric("attendanceRecorded", events.Count(@event => @event.RequiresAttendance && @event.Recorded))
            ],
            table, []);
    }

    /// <summary>Cumpleaños del periodo y, si está configurado, los próximos (P04: días hacia adelante).</summary>
    private async Task<ReportResponse> BirthdaysAsync(GetReportQuery request, IQueryable<GroupMembership> memberships, OperationalSettings settings, CancellationToken cancellationToken)
    {
        var people = await memberships
            .Where(membership => membership.EndedAt == null && membership.Attendee.IsActive && membership.Attendee.BirthDate != null)
            .Select(membership => new { membership.AttendeeId, Name = membership.Attendee.FirstName + " " + membership.Attendee.LastName, membership.Attendee.BirthDate })
            .ToListAsync(cancellationToken);

        var inPeriod = new List<(Guid Id, string Name, DateOnly Date)>();
        foreach (var person in people)
        {
            for (var year = request.From.Year; year <= request.To.Year; year++)
            {
                var date = LocalCalendar.BirthdayIn(person.BirthDate!.Value, year);
                if (date >= request.From && date <= request.To)
                {
                    inPeriod.Add((person.AttendeeId, person.Name, date));
                }
            }
        }

        var table = new ReportTable(["day", "month"], inPeriod.OrderBy(item => item.Date)
            .Select(item => new ReportRow(item.Name, [item.Date.Day, item.Date.Month])).ToList());

        var signal = new ReportSignal("upcomingBirthdays", false, null, []);
        if (settings.UpcomingBirthdaysDays is { } days)
        {
            var today = LocalCalendar.Today(_timeProvider);
            signal = new ReportSignal("upcomingBirthdays", true, $"Próximos {days} días", people
                .Select(person => (person.AttendeeId, person.Name, Date: LocalCalendar.NextBirthday(person.BirthDate!.Value, today)))
                .Where(item => item.Date.DayNumber - today.DayNumber <= days)
                .OrderBy(item => item.Date)
                .Select(item => new SignalItem(item.AttendeeId, item.Name, item.Date.ToString("dd/MM", System.Globalization.CultureInfo.InvariantCulture)))
                .ToList());
        }

        return new ReportResponse(ReportFamily.Birthdays, request.From, request.To,
            [new ReportMetric("birthdays", inPeriod.Count)], table, [signal]);
    }

    /// <summary>§17: el asistente pertenece al público del evento con su grupo de ese momento.</summary>
    private static bool Eligible(EventRow @event, MembershipRow membership) => membership.IsActive && @event.Audience switch
    {
        EventAudience.AllMinistry => true,
        EventAudience.PrimaryGroupAndServers => membership.IsServer,
        EventAudience.Generations => @event.GenerationIds.Contains(membership.GenerationId),
        EventAudience.Groups => @event.GroupIds.Contains(membership.GroupId),
        _ => false
    };

    /// <summary>Semanas de lunes a domingo que cubren el periodo, recortadas a sus límites.</summary>
    private static List<(DateOnly Start, DateOnly End)> Weeks(DateOnly from, DateOnly to)
    {
        var weeks = new List<(DateOnly, DateOnly)>();
        var start = from;

        while (start <= to)
        {
            var daysToSunday = (7 - (int)start.DayOfWeek) % 7;
            var end = start.AddDays(daysToSunday);
            weeks.Add((start, end > to ? to : end));
            start = end.AddDays(1);
        }

        return weeks;
    }

    private sealed record EventRow(Guid Id, DateTime StartsAt, EventAudience Audience, List<Guid> GenerationIds, List<Guid> GroupIds);

    private sealed record MembershipRow(Guid AttendeeId, string Name, bool IsServer, bool IsActive, Guid GroupId, Guid GenerationId, DateTime StartedAt, DateTime? EndedAt);

    private sealed record Fact(DateOnly Date, Guid AttendeeId, string Name, AttendanceMark? Mark);
}

public class GetReportQueryValidator : AbstractValidator<GetReportQuery>
{
    public GetReportQueryValidator()
    {
        RuleFor(query => query.Family).IsInEnum();
        RuleFor(query => query.To).GreaterThanOrEqualTo(query => query.From).WithMessage("To must be after From");
        RuleFor(query => query).Must(query => query.To.DayNumber - query.From.DayNumber <= 366).WithMessage("The period cannot exceed one year");
    }
}
