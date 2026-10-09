using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance;

public enum PersonType
{
    Attendee = 1,
    User = 2
}

/// <param name="Context">Grupo de la persona en la fecha del evento, o su rol si no pertenece a un grupo.</param>
/// <param name="GroupId">Grupo de ese momento (para filtrar), si tiene.</param>
public sealed record AttendancePerson(
    PersonType PersonType,
    Guid PersonId,
    string FullName,
    UserRole? Role,
    bool IsServer,
    Guid? GroupId,
    string? Context,
    AttendanceMark? Mark);

/// <summary>
/// Población de asistencia de un evento (§17, §19, HU-17) y qué personas puede marcar cada usuario (§20).
/// </summary>
/// <remarks>
/// Decisión P06: los asistentes se toman con el grupo al que pertenecían en la fecha de inicio del evento
/// (historial de traslados); quien ya tiene una marca siempre aparece. La ausencia no se guarda: es no tener marca.
/// </remarks>
public sealed class AttendanceSheetService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public AttendanceSheetService(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    /// <summary>
    /// Personas que el usuario puede marcar en el evento. El director, el creador y los encargados marcan al público completo
    /// del evento; los demás, solo a las personas de sus grupos (también ven solo esas: no se expone a nadie más).
    /// </summary>
    public async Task<IReadOnlyList<AttendancePerson>> MarkablePeopleAsync(Event @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var people = await EligiblePeopleAsync(@event, cancellationToken);

        if (_access.CanAdministerEvent(@event))
        {
            return people;
        }

        var scope = await _access.GetScopeAsync(cancellationToken);
        var groupIds = scope.OperationalGroupIds;
        var teamUserIds = await _dbContext.GroupAssignments
            .Where(assignment => groupIds.Contains(assignment.GroupId) && assignment.Responsibility != GroupResponsibility.Coordinator)
            .Select(assignment => assignment.UserId)
            .ToListAsync(cancellationToken);

        return people.Where(person => person.PersonType == PersonType.Attendee
                ? person.GroupId is { } groupId && groupIds.Contains(groupId)
                : teamUserIds.Contains(person.PersonId))
            .ToList();
    }

    /// <summary>Público del evento con su marca actual, más quienes ya tienen marca aunque ya no cumplan el público.</summary>
    public async Task<IReadOnlyList<AttendancePerson>> EligiblePeopleAsync(Event @event, CancellationToken cancellationToken)
    {
        var start = @event.StartsAt;
        var eventYear = LocalCalendar.LocalDateOf(start).Year;
        var groupIds = @event.Groups.Select(item => item.GroupId).ToList();
        var generationIds = @event.Generations.Select(item => item.GenerationId).ToList();

        var attendeeMarks = await _dbContext.AttendeeAttendances.Where(item => item.EventId == @event.Id)
            .ToDictionaryAsync(item => item.AttendeeId, item => item.Mark, cancellationToken);
        var userMarks = await _dbContext.UserAttendances.Where(item => item.EventId == @event.Id)
            .ToDictionaryAsync(item => item.UserId, item => item.Mark, cancellationToken);

        var memberships = _dbContext.GroupMemberships.IgnoreQueryFilters()
            .Where(membership => membership.StartedAt <= start && (membership.EndedAt == null || membership.EndedAt > start) && !membership.Attendee.IsDeleted);

        var audienceMemberships = @event.Audience switch
        {
            EventAudience.AllMinistry => memberships.Where(membership => membership.Attendee.IsActive),
            EventAudience.PrimaryGroupAndServers => memberships.Where(membership => membership.Attendee.IsActive && membership.Attendee.IsServer),
            EventAudience.Generations => memberships.Where(membership => membership.Attendee.IsActive && generationIds.Contains(membership.Group.GenerationId)),
            EventAudience.Groups => memberships.Where(membership => membership.Attendee.IsActive && groupIds.Contains(membership.GroupId)),
            _ => memberships.Where(_ => false)
        };

        var markedAttendeeIds = attendeeMarks.Keys.ToList();
        var attendees = await audienceMemberships
            .Union(memberships.Where(membership => markedAttendeeIds.Contains(membership.AttendeeId)))
            .Select(membership => new
            {
                membership.AttendeeId,
                membership.Attendee.FirstName,
                membership.Attendee.LastName,
                membership.Attendee.IsServer,
                membership.GroupId,
                Context = membership.Group.Generation.Name + " · " + membership.Group.Name
            })
            .ToListAsync(cancellationToken);

        var people = attendees
            .DistinctBy(attendee => attendee.AttendeeId)
            .Select(attendee => new AttendancePerson(PersonType.Attendee, attendee.AttendeeId, $"{attendee.FirstName} {attendee.LastName}", null,
                attendee.IsServer, attendee.GroupId, attendee.Context, attendeeMarks.TryGetValue(attendee.AttendeeId, out var mark) ? mark : null))
            .ToList();

        // Asistentes marcados que ya no tienen pertenencia en esa fecha (por ejemplo, registrados después).
        var missing = markedAttendeeIds.Except(people.Select(person => person.PersonId)).ToList();
        if (missing.Count > 0)
        {
            var extra = await _dbContext.Attendees.Where(attendee => missing.Contains(attendee.Id))
                .Select(attendee => new { attendee.Id, attendee.FirstName, attendee.LastName, attendee.IsServer })
                .ToListAsync(cancellationToken);
            people.AddRange(extra.Select(attendee => new AttendancePerson(PersonType.Attendee, attendee.Id, $"{attendee.FirstName} {attendee.LastName}",
                null, attendee.IsServer, null, null, attendeeMarks[attendee.Id])));
        }

        people.AddRange(await EligibleUsersAsync(@event, eventYear, groupIds, generationIds, userMarks, cancellationToken));

        return people.OrderBy(person => person.FullName, StringComparer.CurrentCulture).ToList();
    }

    private async Task<IEnumerable<AttendancePerson>> EligibleUsersAsync(
        Event @event, int eventYear, List<Guid> groupIds, List<Guid> generationIds, Dictionary<Guid, AttendanceMark> marks, CancellationToken cancellationToken)
    {
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var ministry = directory.Values.Where(user => user.IsActive && user.IsMinistry).ToList();

        // Para públicos de grupos o generaciones, los usuarios que pertenecen a esos grupos: líderes y aprendices.
        var teams = await _dbContext.GroupAssignments
            .Where(assignment => assignment.Responsibility != GroupResponsibility.Coordinator
                && ((@event.Audience == EventAudience.Groups && groupIds.Contains(assignment.GroupId))
                    || (@event.Audience == EventAudience.Generations && generationIds.Contains(assignment.Group.GenerationId) && assignment.Group.Year == eventYear)))
            .Select(assignment => new { assignment.UserId, assignment.GroupId, Context = assignment.Group.Generation.Name + " · " + assignment.Group.Name })
            .ToListAsync(cancellationToken);

        var eligible = @event.Audience switch
        {
            EventAudience.AllMinistry or EventAudience.PrimaryGroup or EventAudience.PrimaryGroupAndServers => ministry,
            EventAudience.DirectorsAndCoordinators => ministry.Where(user => user.Role is UserRole.Director or UserRole.Coordinator).ToList(),
            EventAudience.DirectorsCoordinatorsAndLeaders => ministry.Where(user => user.Role is UserRole.Director or UserRole.Coordinator or UserRole.Leader).ToList(),
            _ => ministry.Where(user => teams.Any(team => team.UserId == user.Id)).ToList()
        };

        var ids = eligible.Select(user => user.Id).Union(marks.Keys).Distinct();

        return ids.Select(id =>
        {
            var user = directory.GetValueOrDefault(id);
            var team = teams.FirstOrDefault(item => item.UserId == id);

            return new AttendancePerson(PersonType.User, id, user?.FullName ?? "Usuario eliminado", user?.Role, false,
                team?.GroupId, team?.Context, marks.TryGetValue(id, out var mark) ? mark : null);
        });
    }
}
