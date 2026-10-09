using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

/// <summary>
/// Asignación y traslado de asistentes entre grupos (§13, HU-06). Siempre deja exactamente un grupo vigente:
/// cierra la pertenencia actual y abre la nueva en la misma transacción. Los registros anteriores conservan su
/// contexto porque se interpretan con la pertenencia vigente en su fecha.
/// </summary>
public sealed class MembershipService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly AuditTrail _audit;
    private readonly NotificationDispatcher _notifications;
    private readonly TimeProvider _timeProvider;

    public MembershipService(IApplicationDbContext dbContext, AuditTrail audit, NotificationDispatcher notifications, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _audit = audit;
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    /// <summary>Grupo activo de destino, con su generación.</summary>
    /// <exception cref="ValidationException">El grupo no existe o no está activo.</exception>
    public async Task<Group> FindActiveGroupAsync(Guid groupId, CancellationToken cancellationToken) =>
        await _dbContext.Groups.Include(group => group.Generation)
            .FirstOrDefaultAsync(group => group.Id == groupId && group.IsActive, cancellationToken)
        ?? throw ValidationException.For("GroupId", "Select an active group.");

    /// <summary>
    /// Agrega la pertenencia inicial de un asistente nuevo. El llamador guarda los cambios.
    /// </summary>
    public void AddInitialMembership(Attendee attendee, Group group)
    {
        ArgumentNullException.ThrowIfNull(attendee);
        ArgumentNullException.ThrowIfNull(group);

        _dbContext.GroupMemberships.Add(new GroupMembership
        {
            AttendeeId = attendee.Id,
            GroupId = group.Id,
            StartedAt = _timeProvider.GetUtcNow().UtcDateTime
        });
    }

    /// <summary>
    /// Traslada en una transacción: primero cierra la vigente (el índice único filtrado admite una sola) y luego
    /// abre la nueva. Si algo falla, el asistente conserva su grupo anterior (HU-06, caso 4).
    /// </summary>
    /// <returns><c>false</c> si ya pertenece a ese grupo (no hay nada que trasladar).</returns>
    public async Task<bool> TransferAsync(Attendee attendee, Group destination, Func<CancellationToken, Task>? alsoInTransaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attendee);
        ArgumentNullException.ThrowIfNull(destination);

        var current = await _dbContext.GroupMemberships
            .Include(membership => membership.Group).ThenInclude(group => group.Generation)
            .FirstOrDefaultAsync(membership => membership.AttendeeId == attendee.Id && membership.EndedAt == null, cancellationToken);

        if (current?.GroupId == destination.Id)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.InTransactionAsync(async token =>
        {
            if (current is not null)
            {
                current.EndedAt = now;
                await _dbContext.SaveChangesAsync(token);
            }

            _dbContext.GroupMemberships.Add(new GroupMembership { AttendeeId = attendee.Id, GroupId = destination.Id, StartedAt = now });

            if (alsoInTransaction is not null)
            {
                await alsoInTransaction(token);
            }

            _audit.Record(AuditModule.Assignments, AuditActions.Transferred, attendee.Id, attendee.FullName,
                current is null ? null : new { Group = Describe(current.Group) },
                new { Group = Describe(destination) });

            await _dbContext.SaveChangesAsync(token);
        }, cancellationToken);

        return true;
    }

    /// <summary>Avisa al equipo del grupo de destino que tiene un asistente nuevo (§23).</summary>
    public async Task NotifyAssignedAsync(Attendee attendee, Group group, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attendee);
        ArgumentNullException.ThrowIfNull(group);

        var team = await _dbContext.GroupAssignments
            .Where(assignment => assignment.GroupId == group.Id)
            .Select(assignment => assignment.UserId)
            .ToListAsync(cancellationToken);

        await _notifications.NotifyAsync(team, NotificationKind.AttendeeAssigned, "Nuevo asistente en tu grupo",
            $"{attendee.FullName} se unió a {group.Generation.Name} · {group.Name}.", attendeeId: attendee.Id, cancellationToken: cancellationToken);
    }

    public static string Describe(Group group) => $"{group.Generation.Name} · {group.Name} · {group.Year}";
}
