using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Commands;

/// <summary>
/// Define el equipo del grupo: coordinadores, líderes y aprendices. El director lo define completo; un
/// coordinador del grupo administra líderes y aprendices. Un coordinador puede ser también líder.
/// </summary>
public class SetGroupTeamCommand : IRequest
{
    public Guid Id { get; set; }
    public List<Guid> CoordinatorIds { get; set; } = [];
    public List<Guid> LeaderIds { get; set; } = [];
    public List<Guid> ApprenticeIds { get; set; } = [];
}

public class SetGroupTeamCommandHandler : IRequestHandler<SetGroupTeamCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;
    private readonly IUserDirectory _userDirectory;

    public SetGroupTeamCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        AuditTrail audit,
        IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
        _userDirectory = userDirectory;
    }

    public async Task<Unit> Handle(SetGroupTeamCommand request, CancellationToken cancellationToken = default)
    {
        var group = await GroupCommandHelpers.FindEditableAsync(_dbContext, _access, request.Id, cancellationToken);
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

        _audit.Record(AuditModule.Groups, AuditActions.TeamChanged, group.Id, GroupCommandHelpers.Describe(group, group.Generation), before, Team(requested, directory));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    /// <summary>
    /// Cada responsabilidad exige el rol de la cuenta: coordinador con rol Coordinador; líder con rol Líder o
    /// Coordinador; aprendiz con rol Aprendiz. Solo cuentas activas.
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
}

public class SetGroupTeamCommandValidator : AbstractValidator<SetGroupTeamCommand>
{
    public SetGroupTeamCommandValidator()
    {
        RuleFor(command => command.CoordinatorIds.Count + command.LeaderIds.Count + command.ApprenticeIds.Count)
            .LessThanOrEqualTo(100).WithMessage("Too many team members");
    }
}
