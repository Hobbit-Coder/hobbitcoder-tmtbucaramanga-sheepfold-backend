using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups;

public sealed record PendingReassignment(Guid AttendeeId, string FullName, Guid CurrentGroupId, string CurrentGroup, int CurrentYear, Guid GenerationId, string GenerationName);

public sealed record RenewalOverviewResponse(int Year, IReadOnlyList<PendingReassignment> PendingAttendees);

/// <summary>
/// Organización de los grupos de un año (§12, HU-07): asistentes activos cuyo grupo vigente es de un año
/// anterior y deben quedar en un grupo del año nuevo. Los grupos del año se consultan con <see cref="GetGroupsQuery"/>.
/// </summary>
public class GetRenewalOverviewQuery : IRequest<RenewalOverviewResponse>
{
    public int Year { get; set; }
}

/// <summary>
/// Guarda las asignaciones al grupo del año nuevo de varios asistentes. Cada una es un traslado: el asistente
/// nunca queda sin grupo y su historia anterior se conserva. Cada grupo de un año anterior que queda sin
/// asistentes vigentes se desactiva (HU-07): sigue disponible para consulta histórica.
/// </summary>
public class SaveRenewalAssignmentsCommand : IRequest
{
    public int Year { get; set; }
    public List<RenewalAssignment> Assignments { get; set; } = [];
}

public sealed record RenewalAssignment(Guid AttendeeId, Guid GroupId);

public class RenewalHandlers :
    IRequestHandler<GetRenewalOverviewQuery, RenewalOverviewResponse>,
    IRequestHandler<SaveRenewalAssignmentsCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly MembershipService _memberships;
    private readonly AuditTrail _audit;

    public RenewalHandlers(IApplicationDbContext dbContext, MinistryAccess access, MembershipService memberships, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _memberships = memberships;
        _audit = audit;
    }

    public async Task<RenewalOverviewResponse> Handle(GetRenewalOverviewQuery request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var pending = await _dbContext.GroupMemberships
            .Where(membership => membership.EndedAt == null && membership.Attendee.IsActive && membership.Group.Year < request.Year)
            .OrderBy(membership => membership.Group.Generation.SortOrder).ThenBy(membership => membership.Attendee.FirstName)
            .Select(membership => new PendingReassignment(
                membership.AttendeeId, membership.Attendee.FirstName + " " + membership.Attendee.LastName,
                membership.GroupId, membership.Group.Name, membership.Group.Year, membership.Group.GenerationId, membership.Group.Generation.Name))
            .ToListAsync(cancellationToken);

        return new RenewalOverviewResponse(request.Year, pending);
    }

    public async Task<Unit> Handle(SaveRenewalAssignmentsCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        // Se valida cada asignación antes de trasladar: un error no deja la renovación a medias.
        var planned = new List<(Domain.Entities.Attendee Attendee, Domain.Entities.Group Group)>();

        foreach (var assignment in request.Assignments)
        {
            var group = await _memberships.FindActiveGroupAsync(assignment.GroupId, cancellationToken);

            if (group.Year != request.Year)
            {
                throw ValidationException.For(nameof(assignment.GroupId), $"The group is not from {request.Year}.");
            }

            var attendee = await _dbContext.Attendees.FirstOrDefaultAsync(item => item.Id == assignment.AttendeeId, cancellationToken)
                ?? throw new NotFoundException("Attendee", assignment.AttendeeId);

            // §9: si la nueva generación exige otro contacto, el traslado se hace desde la ficha agregándolo.
            var kinds = await _dbContext.AttendeeContacts.Where(contact => contact.AttendeeId == attendee.Id).Select(contact => contact.Kind).ToListAsync(cancellationToken);
            if (!kinds.Contains(group.Generation.RequiredContactKind))
            {
                throw new ConflictException($"{attendee.FullName} needs the contact required by {group.Generation.Name}. Transfer them from their profile adding it.");
            }

            planned.Add((attendee, group));
        }

        var attendeeIds = planned.Select(item => item.Attendee.Id).ToList();
        var previousGroupIds = await _dbContext.GroupMemberships
            .Where(membership => membership.EndedAt == null && attendeeIds.Contains(membership.AttendeeId) && membership.Group.Year < request.Year)
            .Select(membership => membership.GroupId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var (attendee, group) in planned)
        {
            if (await _memberships.TransferAsync(attendee, group, null, cancellationToken))
            {
                await _memberships.NotifyAssignedAsync(attendee, group, cancellationToken);
            }
        }

        await DeactivateEmptiedGroupsAsync(previousGroupIds, cancellationToken);

        return Unit.Value;
    }

    private async Task DeactivateEmptiedGroupsAsync(List<Guid> groupIds, CancellationToken cancellationToken)
    {
        var emptied = await _dbContext.Groups
            .Include(group => group.Generation)
            .Where(group => groupIds.Contains(group.Id) && group.IsActive
                && !_dbContext.GroupMemberships.Any(membership => membership.GroupId == group.Id && membership.EndedAt == null))
            .ToListAsync(cancellationToken);

        foreach (var group in emptied)
        {
            group.IsActive = false;
            _audit.Record(AuditModule.Groups, AuditActions.Deactivated, group.Id, $"{group.Generation.Name} · {group.Name} · {group.Year}",
                new { group.Name, IsActive = true }, new { group.Name, group.IsActive });
        }

        if (emptied.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public class SaveRenewalAssignmentsCommandValidator : AbstractValidator<SaveRenewalAssignmentsCommand>
{
    public SaveRenewalAssignmentsCommandValidator()
    {
        RuleFor(command => command.Year).InclusiveBetween(2000, 2200);
        RuleFor(command => command.Assignments).NotEmpty().Must(items => items.Count <= 500).WithMessage("At most 500 assignments per request");
        RuleFor(command => command.Assignments).Must(items => items.Select(item => item.AttendeeId).Distinct().Count() == items.Count)
            .WithMessage("Each attendee can be assigned once");
    }
}
