using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;

/// <summary>
/// Alcance ministerial del usuario autenticado (§4, §28.1): su rol (del token de Identity) y sus
/// responsabilidades en grupos (de Sheepfold). Todos los handlers lo usan para filtrar consultas y autorizar
/// acciones, así que la regla vive en un solo lugar y nunca depende de lo que muestre la interfaz.
/// </summary>
/// <remarks>
/// Se registra por petición: las responsabilidades se leen una vez y se reutilizan durante la petición.
/// </remarks>
public sealed class MinistryAccess
{
    public const string RegisterAttendeesPermission = "RegisterAttendees";

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private AccessScope? _scope;

    public MinistryAccess(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public Guid UserId => Guid.TryParse(_currentUserService.UserId, out var userId)
        ? userId
        : throw new UnauthorizedAccessException("No hay un usuario autenticado.");

    public UserRole? Role => _currentUserService.Role;

    public bool IsDirector => Role == UserRole.Director;

    public bool IsAdmin => Role == UserRole.Admin;

    /// <summary>Director, coordinador, líder o aprendiz.</summary>
    public bool IsMinistry => Role is UserRole.Director or UserRole.Coordinator or UserRole.Leader or UserRole.Apprentice;

    /// <summary>Puede registrar seguimientos (§21): director, coordinador o líder; nunca el aprendiz.</summary>
    public bool CanFollowUp => Role is UserRole.Director or UserRole.Coordinator or UserRole.Leader;

    /// <summary>§7: el director siempre; los demás solo con la designación. La designación no amplía la edición.</summary>
    public bool CanRegisterAttendees =>
        IsDirector || (IsMinistry && _currentUserService.HasPermission(RegisterAttendeesPermission));

    /// <summary>El administrador y cualquier cuenta sin rol ministerial no acceden a información ministerial (§4.5).</summary>
    public void EnsureMinistry()
    {
        if (!IsMinistry)
        {
            throw new ForbiddenAccessException();
        }
    }

    public void EnsureDirector()
    {
        if (!IsDirector)
        {
            throw new ForbiddenAccessException();
        }
    }

    public void EnsureAdmin()
    {
        if (!IsAdmin)
        {
            throw new ForbiddenAccessException();
        }
    }

    public async Task<AccessScope> GetScopeAsync(CancellationToken cancellationToken)
    {
        EnsureMinistry();

        if (_scope is not null)
        {
            return _scope;
        }

        var userId = UserId;
        var groups = await _dbContext.GroupAssignments
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => new ScopeGroup(
                assignment.GroupId, assignment.Group.Year, assignment.Group.GenerationId, assignment.Group.IsActive, assignment.Responsibility))
            .ToListAsync(cancellationToken);

        _scope = new AccessScope(userId, Role!.Value, groups);
        return _scope;
    }

    /// <summary>Asistentes que el usuario consulta y edita (§4): los de sus grupos, o todos para el director.</summary>
    public async Task<IQueryable<Attendee>> AttendeesInScopeAsync(IQueryable<Attendee> attendees, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(cancellationToken);

        if (scope.IsDirector)
        {
            return attendees;
        }

        var groupIds = scope.OperationalGroupIds;
        return attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && groupIds.Contains(membership.GroupId)));
    }

    /// <exception cref="NotFoundException">El asistente no existe o no está en el alcance (no se distingue).</exception>
    public async Task<Attendee> FindAttendeeInScopeAsync(Guid attendeeId, CancellationToken cancellationToken)
    {
        var attendees = await AttendeesInScopeAsync(_dbContext.Attendees, cancellationToken);

        return await attendees.FirstOrDefaultAsync(attendee => attendee.Id == attendeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Attendee), attendeeId);
    }

    /// <summary>
    /// Eventos que el usuario consulta (§16): todos para el director; para los demás, los que creó o tiene
    /// encargados y los publicados (no borradores) cuyo público incluye su rol, sus generaciones o sus grupos.
    /// </summary>
    public async Task<IQueryable<Event>> VisibleEventsAsync(IQueryable<Event> events, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(cancellationToken);

        if (scope.IsDirector)
        {
            return events;
        }

        var userId = scope.UserId.ToString();
        var managerId = scope.UserId;
        var role = scope.Role;
        var groupIds = scope.OperationalGroupIds;
        var generationIds = scope.ActiveGenerationIds;
        var isCoordinator = role == UserRole.Coordinator;
        var isCoordinatorOrLeader = role is UserRole.Coordinator or UserRole.Leader;

        return events.Where(@event =>
            @event.CreatedBy == userId
            || @event.Managers.Any(manager => manager.UserId == managerId)
            || (@event.Status != EventStatus.Draft && (
                @event.Audience == EventAudience.AllMinistry
                || @event.Audience == EventAudience.PrimaryGroup
                || @event.Audience == EventAudience.PrimaryGroupAndServers
                || (@event.Audience == EventAudience.DirectorsAndCoordinators && isCoordinator)
                || (@event.Audience == EventAudience.DirectorsCoordinatorsAndLeaders && isCoordinatorOrLeader)
                || (@event.Audience == EventAudience.Generations && @event.Generations.Any(item => generationIds.Contains(item.GenerationId)))
                || (@event.Audience == EventAudience.Groups && @event.Groups.Any(item => groupIds.Contains(item.GroupId))))));
    }

    /// <exception cref="NotFoundException">El evento no existe o no es visible (no se distingue).</exception>
    public async Task<Event> FindVisibleEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var events = await VisibleEventsAsync(_dbContext.Events.Include(@event => @event.Managers), cancellationToken);

        return await events.FirstOrDefaultAsync(@event => @event.Id == eventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), eventId);
    }

    /// <summary>§16 y §4.1: administra el evento su creador, sus encargados y el director.</summary>
    public bool CanAdministerEvent(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return IsDirector
            || @event.CreatedBy == UserId.ToString()
            || @event.Managers.Any(manager => manager.UserId == UserId);
    }

    /// <summary>
    /// Usuarios inferiores a quienes puede hacer seguimiento (§21): el director a coordinadores, líderes y
    /// aprendices; el coordinador a líderes y aprendices de sus grupos; el líder a los aprendices de su grupo.
    /// </summary>
    public async Task<HashSet<Guid>> InferiorUserIdsAsync(IReadOnlyDictionary<Guid, DirectoryUser> directory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(directory);
        var scope = await GetScopeAsync(cancellationToken);

        if (scope.IsDirector)
        {
            return directory.Values
                .Where(user => user.Role is UserRole.Coordinator or UserRole.Leader or UserRole.Apprentice && user.Id != scope.UserId)
                .Select(user => user.Id)
                .ToHashSet();
        }

        var responsibilities = scope.Role switch
        {
            UserRole.Coordinator => new[] { GroupResponsibility.Leader, GroupResponsibility.Apprentice },
            UserRole.Leader => [GroupResponsibility.Apprentice],
            _ => []
        };

        if (responsibilities.Length == 0)
        {
            return [];
        }

        var groupIds = scope.ActiveManagedGroupIds;
        var userIds = await _dbContext.GroupAssignments
            .Where(assignment => groupIds.Contains(assignment.GroupId) && responsibilities.Contains(assignment.Responsibility))
            .Select(assignment => assignment.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return userIds.Where(userId => userId != scope.UserId).ToHashSet();
    }

    /// <summary>
    /// Seguimientos cuyos datos generales puede consultar (§22): el director todos; los demás, los que escribió y
    /// los de personas de su alcance. Nunca los propios: la persona atendida no conoce su seguimiento (§28.12).
    /// El aprendiz no tiene acceso pastoral.
    /// </summary>
    public async Task<IQueryable<FollowUp>> VisibleFollowUpsAsync(
        IQueryable<FollowUp> followUps, IReadOnlyDictionary<Guid, DirectoryUser> directory, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(cancellationToken);
        var me = scope.UserId;
        var notAboutMe = followUps.Where(followUp => followUp.SubjectUserId == null || followUp.SubjectUserId != me);

        if (scope.IsDirector)
        {
            return notAboutMe;
        }

        if (!CanFollowUp)
        {
            return followUps.Where(_ => false);
        }

        var author = me.ToString();
        var groupIds = scope.ManagedGroupIds;
        var inferiorUserIds = await InferiorUserIdsAsync(directory, cancellationToken);

        return notAboutMe.Where(followUp =>
            followUp.CreatedBy == author
            || (followUp.SubjectAttendeeId != null && followUp.SubjectAttendee!.Memberships.Any(
                membership => membership.EndedAt == null && groupIds.Contains(membership.GroupId)))
            // Un realizado sigue asociado al grupo de la reunión aunque la persona se haya trasladado (HU-07).
            || (followUp.GroupId != null && groupIds.Contains(followUp.GroupId.Value))
            || (followUp.SubjectUserId != null && inferiorUserIds.Contains(followUp.SubjectUserId.Value)));
    }

    /// <summary>
    /// Contenido completo de un seguimiento (§22): su autor y el director. Si no es confidencial, también quien
    /// consulta sus datos generales por jerarquía.
    /// </summary>
    public bool CanReadFollowUpContent(FollowUp followUp)
    {
        ArgumentNullException.ThrowIfNull(followUp);

        return IsDirector || followUp.CreatedBy == UserId.ToString() || !followUp.IsConfidential;
    }

    /// <summary>Editar, completar o cambiar la confidencialidad: el autor o el director.</summary>
    public bool CanEditFollowUp(FollowUp followUp)
    {
        ArgumentNullException.ThrowIfNull(followUp);

        return IsDirector || followUp.CreatedBy == UserId.ToString();
    }
}

public sealed record ScopeGroup(Guid GroupId, int Year, Guid GenerationId, bool IsActive, GroupResponsibility Responsibility);

/// <summary>Responsabilidades del usuario en grupos, de todos los años (para la consulta histórica).</summary>
public sealed class AccessScope
{
    public AccessScope(Guid userId, UserRole role, IReadOnlyList<ScopeGroup> groups)
    {
        UserId = userId;
        Role = role;
        Groups = groups;

        CoordinatedGroupIds = Ids(GroupResponsibility.Coordinator);
        LedGroupIds = Ids(GroupResponsibility.Leader);
        ApprenticeGroupIds = Ids(GroupResponsibility.Apprentice);
        ManagedGroupIds = [.. CoordinatedGroupIds, .. LedGroupIds];
        OperationalGroupIds = [.. ManagedGroupIds, .. ApprenticeGroupIds];
        ActiveManagedGroupIds = groups.Where(group => group.IsActive && ManagedGroupIds.Contains(group.GroupId)).Select(group => group.GroupId).ToHashSet();
        ActiveOperationalGroupIds = groups.Where(group => group.IsActive).Select(group => group.GroupId).ToHashSet();
        ActiveGenerationIds = groups.Where(group => group.IsActive).Select(group => group.GenerationId).ToHashSet();
    }

    public Guid UserId { get; }
    public UserRole Role { get; }
    public IReadOnlyList<ScopeGroup> Groups { get; }
    public bool IsDirector => Role == UserRole.Director;

    public HashSet<Guid> CoordinatedGroupIds { get; }
    public HashSet<Guid> LedGroupIds { get; }
    public HashSet<Guid> ApprenticeGroupIds { get; }

    /// <summary>Grupos que coordina o lidera: seguimientos y administración de grupos.</summary>
    public HashSet<Guid> ManagedGroupIds { get; }

    /// <summary>Grupos donde además es aprendiz: consulta y edición de asistentes y asistencia.</summary>
    public HashSet<Guid> OperationalGroupIds { get; }

    public HashSet<Guid> ActiveManagedGroupIds { get; }
    public HashSet<Guid> ActiveOperationalGroupIds { get; }
    public HashSet<Guid> ActiveGenerationIds { get; }

    /// <summary>Grupos activos para los que puede crear eventos (§16): los que coordina o lidera.</summary>
    public HashSet<Guid> EventGroupIds => Role is UserRole.Coordinator or UserRole.Leader ? ActiveManagedGroupIds : [];

    private HashSet<Guid> Ids(GroupResponsibility responsibility) =>
        Groups.Where(group => group.Responsibility == responsibility).Select(group => group.GroupId).ToHashSet();
}
