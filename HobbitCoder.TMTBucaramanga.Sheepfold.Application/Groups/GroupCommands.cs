using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups;

/// <summary>
/// El director crea un grupo de un año y una generación (§11, §12). La renovación anual es crear los grupos del
/// año nuevo con este mismo comando; no hay un proceso de cierre aparte.
/// </summary>
public class CreateGroupCommand : IRequest<Guid>
{
    public required int Year { get; set; }
    public required Guid GenerationId { get; set; }
    public required string Name { get; set; }
}

/// <summary>
/// Cambia el nombre o el estado del grupo. Inactivarlo exige que ningún asistente lo tenga como grupo vigente
/// (§28.2): primero se trasladan. El año y la generación no cambian: alterarían la historia de sus asistentes.
/// </summary>
public class UpdateGroupCommand : IRequest
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Define el equipo del grupo (§11): coordinadores, líderes y aprendices. El director lo define completo; un
/// coordinador del grupo administra líderes y aprendices. Un coordinador puede ser también líder.
/// </summary>
public class SetGroupTeamCommand : IRequest
{
    public Guid Id { get; set; }
    public List<Guid> CoordinatorIds { get; set; } = [];
    public List<Guid> LeaderIds { get; set; } = [];
    public List<Guid> ApprenticeIds { get; set; } = [];
}

/// <summary>Retira el grupo de las consultas habituales (§26). Solo el director, y sin asistentes vigentes.</summary>
public class RetireGroupCommand : IRequest
{
    public Guid Id { get; set; }
}

public class GroupCommandHandlers :
    IRequestHandler<CreateGroupCommand, Guid>,
    IRequestHandler<UpdateGroupCommand, Unit>,
    IRequestHandler<SetGroupTeamCommand, Unit>,
    IRequestHandler<RetireGroupCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;
    private readonly IUserDirectory _userDirectory;

    public GroupCommandHandlers(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
        _userDirectory = userDirectory;
    }

    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var generation = await _dbContext.Generations.FirstOrDefaultAsync(item => item.Id == request.GenerationId && item.IsActive, cancellationToken)
            ?? throw ValidationException.For(nameof(request.GenerationId), "Select an active generation.");

        var name = request.Name.Trim();
        await EnsureUniqueAsync(request.Year, generation.Id, name, exceptId: null, cancellationToken);

        var group = new Group { Year = request.Year, GenerationId = generation.Id, Name = name, IsActive = true };
        _dbContext.Groups.Add(group);
        _audit.Record(AuditModule.Groups, AuditActions.Created, group.Id, Describe(group, generation), null, new { group.Year, Generation = generation.Name, group.Name, group.IsActive });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return group.Id;
    }

    public async Task<Unit> Handle(UpdateGroupCommand request, CancellationToken cancellationToken = default)
    {
        var group = await FindEditableAsync(request.Id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureUniqueAsync(group.Year, group.GenerationId, name, group.Id, cancellationToken);

        if (group.IsActive && !request.IsActive)
        {
            await EnsureNoCurrentMembersAsync(group.Id, cancellationToken);
        }

        var before = new { group.Name, group.IsActive };
        group.Name = name;
        group.IsActive = request.IsActive;

        var action = AuditActions.ForActivation(before.IsActive, group.IsActive);
        _audit.Record(AuditModule.Groups, action, group.Id, Describe(group, group.Generation), before, new { group.Name, group.IsActive });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(SetGroupTeamCommand request, CancellationToken cancellationToken = default)
    {
        var group = await FindEditableAsync(request.Id, cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var current = await _dbContext.GroupAssignments.Where(assignment => assignment.GroupId == group.Id).ToListAsync(cancellationToken);
        var coordinatorIds = _access.IsDirector
            ? request.CoordinatorIds.Distinct().ToList()
            : current.Where(assignment => assignment.Responsibility == GroupResponsibility.Coordinator).Select(assignment => assignment.UserId).ToList();

        var requested = coordinatorIds.Select(id => (id, GroupResponsibility.Coordinator))
            .Concat(request.LeaderIds.Distinct().Select(id => (id, GroupResponsibility.Leader)))
            .Concat(request.ApprenticeIds.Distinct().Select(id => (id, GroupResponsibility.Apprentice)))
            .ToList();

        ValidateTeam(requested, directory);

        var before = Team(current.Select(assignment => (assignment.UserId, assignment.Responsibility)), directory);
        _dbContext.GroupAssignments.RemoveRange(current.Where(assignment => !requested.Contains((assignment.UserId, assignment.Responsibility))));
        _dbContext.GroupAssignments.AddRange(requested
            .Where(item => !current.Any(assignment => assignment.UserId == item.Item1 && assignment.Responsibility == item.Item2))
            .Select(item => new GroupAssignment { GroupId = group.Id, UserId = item.Item1, Responsibility = item.Item2 }));

        _audit.Record(AuditModule.Groups, AuditActions.TeamChanged, group.Id, Describe(group, group.Generation), before, Team(requested, directory));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(RetireGroupCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var group = await _dbContext.Groups.Include(item => item.Generation).FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Group), request.Id);
        await EnsureNoCurrentMembersAsync(group.Id, cancellationToken);

        _dbContext.Groups.Remove(group);
        _audit.Record(AuditModule.Groups, AuditActions.Retired, group.Id, Describe(group, group.Generation), new { group.Name, group.IsActive }, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    /// <summary>El director, o un coordinador del grupo (§4.2: administra los grupos bajo su coordinación).</summary>
    private async Task<Group> FindEditableAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);
        var group = await _dbContext.Groups.Include(item => item.Generation).FirstOrDefaultAsync(item => item.Id == groupId, cancellationToken);

        if (group is null || (!scope.IsDirector && !scope.OperationalGroupIds.Contains(groupId)))
        {
            throw new NotFoundException(nameof(Group), groupId);
        }

        if (!scope.IsDirector && !scope.CoordinatedGroupIds.Contains(groupId))
        {
            throw new ForbiddenAccessException();
        }

        return group;
    }

    private async Task EnsureUniqueAsync(int year, Guid generationId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.ToUpperInvariant();

        if (await _dbContext.Groups.IgnoreQueryFilters().AnyAsync(group => group.Id != exceptId && group.Year == year
                && group.GenerationId == generationId && group.Name.ToUpper() == normalized, cancellationToken))
        {
            throw new ConflictException($"A group named '{name}' already exists for that year and generation.");
        }
    }

    private async Task EnsureNoCurrentMembersAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var members = await _dbContext.GroupMemberships.CountAsync(membership => membership.GroupId == groupId && membership.EndedAt == null, cancellationToken);

        if (members > 0)
        {
            throw new ConflictException($"The group still has {members} current attendees. Transfer them to another active group first.");
        }
    }

    /// <summary>
    /// Cada responsabilidad exige el rol de la cuenta: coordinador con rol Coordinador; líder con rol Líder o
    /// Coordinador (§4.2); aprendiz con rol Aprendiz. Solo cuentas activas.
    /// </summary>
    private static void ValidateTeam(IEnumerable<(Guid UserId, GroupResponsibility Responsibility)> team, IReadOnlyDictionary<Guid, DirectoryUser> directory)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var (userId, responsibility) in team)
        {
            var user = directory.GetValueOrDefault(userId);
            var allowed = responsibility switch
            {
                GroupResponsibility.Coordinator => user?.Role == UserRole.Coordinator,
                GroupResponsibility.Leader => user?.Role is UserRole.Leader or UserRole.Coordinator,
                _ => user?.Role == UserRole.Apprentice
            };

            if (user is null || !user.IsActive || !allowed)
            {
                failures.Add(new FluentValidation.Results.ValidationFailure(responsibility.ToString(),
                    $"{(user?.FullName ?? "The user")} cannot be assigned as {responsibility}."));
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }

    private static object Team(IEnumerable<(Guid UserId, GroupResponsibility Responsibility)> team, IReadOnlyDictionary<Guid, DirectoryUser> directory) =>
        team.GroupBy(item => item.Responsibility)
            .ToDictionary(group => group.Key.ToString(), group => group.Select(item => directory.NameOf(item.UserId)).Order().ToList());

    private static string Describe(Group group, Generation generation) => $"{generation.Name} · {group.Name} · {group.Year}";
}

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(command => command.Year).InclusiveBetween(2000, 2200);
        RuleFor(command => command.GenerationId).NotEmpty().WithMessage("GenerationId is required");
        RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required").MaximumLength(60);
    }
}

public class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required").MaximumLength(60);
    }
}

public class SetGroupTeamCommandValidator : AbstractValidator<SetGroupTeamCommand>
{
    public SetGroupTeamCommandValidator()
    {
        RuleFor(command => command.CoordinatorIds.Count + command.LeaderIds.Count + command.ApprenticeIds.Count)
            .LessThanOrEqualTo(100).WithMessage("Too many team members");
    }
}
