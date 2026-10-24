using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Reports.Queries;

/// <summary>
/// Reporte de una familia para el periodo, limitado al alcance del usuario: el director, el ministerio
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

        if (!_access.CanViewReports || (request.Family == ReportFamily.FollowUps && !_access.CanFollowUp))
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
    /// Baja asistencia: participaciones / eventos del público con asistencia en la ventana &lt; umbral %.
    /// Tardanzas frecuentes: "Llegó tarde" en la ventana ≥ umbral.
    /// </summary>
    /// <remarks>
    ///     Cada asistente del alcance en cada evento: elegible por el público con su grupo de ese momento o ya
    ///     marcado.
    /// </remarks>
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
            .Select(@event => new EventRow
            {
                Id = @event.Id,
                StartsAt = @event.StartsAt,
                Audience = @event.Audience,
                GenerationIds = @event.Generations.Select(item => item.GenerationId).ToList(),
                GroupIds = @event.Groups.Select(item => item.GroupId).ToList()
            })
            .ToListAsync(cancellationToken);

        var rows = await memberships
            .Where(membership => membership.StartedAt < toUtc && (membership.EndedAt == null || membership.EndedAt > fromUtc))
            .Select(membership => new MembershipRow
            {
                AttendeeId = membership.AttendeeId,
                Name = membership.Attendee.FirstName + " " + membership.Attendee.LastName,
                IsServer = membership.Attendee.IsServer,
                IsActive = membership.Attendee.IsActive,
                GroupId = membership.GroupId,
                GenerationId = membership.Group.GenerationId,
                StartedAt = membership.StartedAt,
                EndedAt = membership.EndedAt
            })
            .ToListAsync(cancellationToken);

        var eventIds = events.Select(@event => @event.Id).ToList();
        var marks = await _dbContext.AttendeeAttendances.Where(item => eventIds.Contains(item.EventId))
            .ToDictionaryAsync(item => (item.EventId, item.AttendeeId), item => item.Mark, cancellationToken);

        var facts = (from @event in events
                     from membership in rows
                     where membership.StartedAt <= @event.StartsAt && (membership.EndedAt == null || membership.EndedAt > @event.StartsAt)
                     let mark = marks.TryGetValue((@event.Id, membership.AttendeeId), out var value) ? value : (AttendanceMark?)null
                     where mark is not null || Eligible(@event, membership)
                     select new Fact { Date = LocalCalendar.LocalDateOf(@event.StartsAt), AttendeeId = membership.AttendeeId, Name = membership.Name, Mark = mark })
            .ToList();

        var inPeriod = facts.Where(fact => fact.Date >= request.From && fact.Date <= request.To).ToList();
        var weeks = Weeks(request.From, request.To);
        var table = new ReportTable
        {
            Columns = ["attended", "late", "participations"],
            Rows = weeks.Select((week, index) =>
        {
            var weekFacts = inPeriod.Where(fact => fact.Date >= week.Start && fact.Date <= week.End).ToList();
            var attended = weekFacts.Count(fact => fact.Mark == AttendanceMark.Attended);
            var late = weekFacts.Count(fact => fact.Mark == AttendanceMark.Late);
            return new ReportRow { Label = $"Semana {index + 1} · {week.Start:dd/MM}", Values = [attended, late, attended + late] };
        }).ToList()
        };

        var servers = rows.Where(row => row.EndedAt == null && row.IsActive && row.IsServer).Select(row => row.AttendeeId).Distinct().Count();

        return new ReportResponse
        {
            Family = ReportFamily.Attendance,
            From = request.From,
            To = request.To,
            Metrics = [
                new ReportMetric { Key = "participations", Value = inPeriod.Count(fact => fact.Mark is not null) },
                new ReportMetric { Key = "late", Value = inPeriod.Count(fact => fact.Mark == AttendanceMark.Late) },
                new ReportMetric { Key = "unmarked", Value = inPeriod.Count(fact => fact.Mark is null) },
                new ReportMetric { Key = "servers", Value = servers },
                new ReportMetric { Key = "events", Value = events.Count(@event => LocalCalendar.LocalDateOf(@event.StartsAt) >= request.From) }
            ],
            Table = table,
            Signals = [LowAttendance(facts, lowWindowStart, request.To, settings), FrequentLateness(facts, lateWindowStart, request.To, settings)]
        };
    }

    private static ReportSignal LowAttendance(List<Fact> facts, DateOnly windowStart, DateOnly to, OperationalSettings settings)
    {
        if (settings.LowAttendanceThresholdPercent is not { } threshold || settings.LowAttendanceWindowDays is not { } days)
        {
            return new ReportSignal { Key = "lowAttendance", Configured = false, Rule = null, Items = [] };
        }

        var items = facts.Where(fact => fact.Date >= windowStart && fact.Date <= to)
            .GroupBy(fact => (fact.AttendeeId, fact.Name))
            .Select(group => (group.Key, Total: group.Count(), Participations: group.Count(fact => fact.Mark is not null)))
            .Where(item => item.Total > 0 && item.Participations * 100 < threshold * item.Total)
            .OrderBy(item => (double)item.Participations / item.Total)
            .Select(item => new SignalItem
            {
                Id = item.Key.AttendeeId,
                Name = item.Key.Name,
                Detail = $"{item.Participations} de {item.Total} eventos ({item.Participations * 100 / item.Total} %)"
            })
            .ToList();

        return new ReportSignal { Key = "lowAttendance", Configured = true, Rule = $"Participación menor al {threshold} % en los últimos {days} días", Items = items };
    }

    private static ReportSignal FrequentLateness(List<Fact> facts, DateOnly windowStart, DateOnly to, OperationalSettings settings)
    {
        if (settings.FrequentLatenessThreshold is not { } threshold || settings.FrequentLatenessWindowDays is not { } days)
        {
            return new ReportSignal { Key = "frequentLateness", Configured = false, Rule = null, Items = [] };
        }

        var items = facts.Where(fact => fact.Date >= windowStart && fact.Date <= to && fact.Mark == AttendanceMark.Late)
            .GroupBy(fact => (fact.AttendeeId, fact.Name))
            .Where(group => group.Count() >= threshold)
            .OrderByDescending(group => group.Count())
            .Select(group => new SignalItem { Id = group.Key.AttendeeId, Name = group.Key.Name, Detail = $"{group.Count()} llegadas tarde" })
            .ToList();

        return new ReportSignal { Key = "frequentLateness", Configured = true, Rule = $"{threshold} o más llegadas tarde en los últimos {days} días", Items = items };
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

        var table = new ReportTable
        {
            Columns = ["attendees", "servers"],
            Rows = current
            .GroupBy(item => (item.Label, item.SortOrder))
            .OrderBy(group => group.Key.SortOrder).ThenBy(group => group.Key.Label)
            .Select(group => new ReportRow { Label = group.Key.Label, Values = [group.Count(), group.Count(item => item.IsServer)] })
            .ToList()
        };

        return new ReportResponse
        {
            Family = ReportFamily.People,
            From = request.From,
            To = request.To,
            Metrics = [
                new ReportMetric { Key = "attendees", Value = current.Count },
                new ReportMetric { Key = "servers", Value = current.Count(item => item.IsServer) },
                new ReportMetric { Key = "groups", Value = table.Rows.Count },
                new ReportMetric { Key = "groupChanges", Value = transfers.Count },
                new ReportMetric { Key = "generationChanges", Value = transfers.Count(transfer => transfer.Previous != transfer.GenerationId) }
            ],
            Table = table,
            Signals = []
        };
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

        var table = new ReportTable
        {
            Columns = ["done", "pending"],
            Rows = rows
            .GroupBy(row => row.CreatedBy)
            .Select(group => new ReportRow
            {
                Label = directory.NameOf(group.Key),
                Values = [done.Count(row => row.CreatedBy == group.Key), pending.Count(row => row.CreatedBy == group.Key)]
            })
            .Where(row => row.Values.Any(value => value > 0))
            .OrderBy(row => row.Label)
            .ToList()
        };

        return new ReportResponse
        {
            Family = ReportFamily.FollowUps,
            From = request.From,
            To = request.To,
            Metrics = [
                new ReportMetric { Key = "done", Value = done.Count },
                new ReportMetric { Key = "pending", Value = pending.Count },
                new ReportMetric { Key = "pendingWithoutDate", Value = pending.Count(row => row.ScheduledDate is null) }
            ],
            Table = table,
            Signals = []
        };
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

        var table = new ReportTable
        {
            Columns = ["inProgress", "finished", "finishedInPeriod"],
            Rows = courses
            .GroupBy(course => course.Course)
            .OrderBy(group => group.Key)
            .Select(group => new ReportRow
            {
                Label = group.Key,
                Values = [
                group.Count(course => course.Status == CourseStatus.InProgress),
                group.Count(course => course.Status == CourseStatus.Finished),
                group.Count(course => course.FinishedOn >= request.From && course.FinishedOn <= request.To)
            ]
            })
            .ToList()
        };

        var inProgress = courses.Where(course => course.Status == CourseStatus.InProgress)
            .OrderBy(course => course.Name)
            .Select(course => new SignalItem { Id = course.AttendeeId, Name = course.Name, Detail = course.Course })
            .ToList();

        return new ReportResponse
        {
            Family = ReportFamily.Courses,
            From = request.From,
            To = request.To,
            Metrics = [
                new ReportMetric { Key = "inProgress", Value = inProgress.Count },
                new ReportMetric { Key = "finished", Value = courses.Count(course => course.Status == CourseStatus.Finished) }
            ],
            Table = table,
            Signals = [new ReportSignal { Key = "coursesInProgress", Configured = true, Rule = null, Items = inProgress }]
        };
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

        var events = await EventQueryHelpers.Filter(visible, null, null, null, request.GenerationId, request.GroupId)
            .Where(@event => @event.StartsAt >= fromUtc && @event.StartsAt < toUtc)
            .Select(@event => new
            {
                Type = @event.EventType.Name,
                @event.Status,
                @event.RequiresAttendance,
                Recorded = _dbContext.AttendeeAttendances.Any(item => item.EventId == @event.Id) || _dbContext.UserAttendances.Any(item => item.EventId == @event.Id)
            })
            .ToListAsync(cancellationToken);

        var table = new ReportTable
        {
            Columns = ["total", "finished", "cancelled", "attendanceRecorded"],
            Rows = events
            .GroupBy(@event => @event.Type)
            .OrderBy(group => group.Key)
            .Select(group => new ReportRow
            {
                Label = group.Key,
                Values = [
                group.Count(),
                group.Count(@event => @event.Status == EventStatus.Finished),
                group.Count(@event => @event.Status == EventStatus.Cancelled),
                group.Count(@event => @event.RequiresAttendance && @event.Recorded)
            ]
            })
            .ToList()
        };

        return new ReportResponse
        {
            Family = ReportFamily.Events,
            From = request.From,
            To = request.To,
            Metrics = [
                new ReportMetric { Key = "total", Value = events.Count },
                new ReportMetric { Key = "published", Value = events.Count(@event => @event.Status == EventStatus.Published) },
                new ReportMetric { Key = "finished", Value = events.Count(@event => @event.Status == EventStatus.Finished) },
                new ReportMetric { Key = "cancelled", Value = events.Count(@event => @event.Status == EventStatus.Cancelled) },
                new ReportMetric { Key = "attendanceRequired", Value = events.Count(@event => @event.RequiresAttendance && @event.Status != EventStatus.Cancelled) },
                new ReportMetric { Key = "attendanceRecorded", Value = events.Count(@event => @event.RequiresAttendance && @event.Recorded) }
            ],
            Table = table,
            Signals = []
        };
    }

    /// <summary>Cumpleaños del periodo y, si está configurado, los próximos (días hacia adelante).</summary>
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

        var table = new ReportTable
        {
            Columns = ["day", "month"],
            Rows = inPeriod.OrderBy(item => item.Date)
            .Select(item => new ReportRow { Label = item.Name, Values = [item.Date.Day, item.Date.Month] }).ToList()
        };

        var signal = new ReportSignal { Key = "upcomingBirthdays", Configured = false, Rule = null, Items = [] };
        if (settings.UpcomingBirthdaysDays is { } days)
        {
            var today = LocalCalendar.Today(_timeProvider);
            signal = new ReportSignal
            {
                Key = "upcomingBirthdays",
                Configured = true,
                Rule = $"Próximos {days} días",
                Items = people
                .Select(person => (person.AttendeeId, person.Name, Date: LocalCalendar.NextBirthday(person.BirthDate!.Value, today)))
                .Where(item => item.Date.DayNumber - today.DayNumber <= days)
                .OrderBy(item => item.Date)
                .Select(item => new SignalItem { Id = item.AttendeeId, Name = item.Name, Detail = item.Date.ToString("dd/MM", System.Globalization.CultureInfo.InvariantCulture) })
                .ToList()
            };
        }

        return new ReportResponse
        {
            Family = ReportFamily.Birthdays,
            From = request.From,
            To = request.To,
            Metrics = [new ReportMetric { Key = "birthdays", Value = inPeriod.Count }],
            Table = table,
            Signals = [signal]
        };
    }

    /// <summary>El asistente pertenece al público del evento con su grupo de ese momento.</summary>
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

    private sealed class EventRow
    {
        public required Guid Id { get; set; }
        public required DateTime StartsAt { get; set; }
        public required EventAudience Audience { get; set; }
        public required List<Guid> GenerationIds { get; set; }
        public required List<Guid> GroupIds { get; set; }
    }

    private sealed class MembershipRow
    {
        public required Guid AttendeeId { get; set; }
        public required string Name { get; set; }
        public required bool IsServer { get; set; }
        public required bool IsActive { get; set; }
        public required Guid GroupId { get; set; }
        public required Guid GenerationId { get; set; }
        public required DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    private sealed class Fact
    {
        public required DateOnly Date { get; set; }
        public required Guid AttendeeId { get; set; }
        public required string Name { get; set; }
        public AttendanceMark? Mark { get; set; }
    }
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
