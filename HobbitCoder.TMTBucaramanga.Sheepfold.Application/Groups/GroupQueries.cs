using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups;

public sealed record GenerationRef(Guid Id, string Name, GenerationProfile Profile);

public sealed record TeamMember(Guid UserId, string FullName, Gender? Gender, GroupResponsibility Responsibility);

public sealed record GroupSummaryResponse(
    Guid Id,
    string Name,
    int Year,
    GenerationRef Generation,
    bool IsActive,
    int AttendeeCount,
    IReadOnlyList<TeamMember> Team);

public sealed record GroupMemberResponse(Guid AttendeeId, string FullName, bool IsServer, bool IsActive, DateTime Since);

/// <param name="ExpectedLeadersComposition">§11: se espera un líder hombre y una líder mujer. Solo informa; nunca bloquea.</param>
public sealed record GroupDetailResponse(
    GroupSummaryResponse Group,
    IReadOnlyList<GroupMemberResponse> Members,
    bool ExpectedLeadersComposition,
    bool CanEdit,
    bool CanManageCoordinators);

public sealed record GroupHistoryEntry(Guid AttendeeId, string FullName, DateTime StartedAt, DateTime? EndedAt);

/// <summary>
/// Grupos visibles (§4): el director, todos; los demás, los grupos donde tienen una responsabilidad (también
/// los de años anteriores, para la consulta histórica).
/// </summary>
public class GetGroupsQuery : IRequest<List<GroupSummaryResponse>>
{
    public int? Year { get; set; }
    public Guid? GenerationId { get; set; }
    public bool? IsActive { get; set; }
}

public class GetGroupByIdQuery : IRequest<GroupDetailResponse>
{
    public Guid Id { get; set; }
}

/// <summary>Pertenencias actuales y anteriores del grupo (historia, §12).</summary>
public class GetGroupHistoryQuery : IRequest<List<GroupHistoryEntry>>
{
    public Guid Id { get; set; }
}

public class GroupQueryHandlers :
    IRequestHandler<GetGroupsQuery, List<GroupSummaryResponse>>,
    IRequestHandler<GetGroupByIdQuery, GroupDetailResponse>,
    IRequestHandler<GetGroupHistoryQuery, List<GroupHistoryEntry>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GroupQueryHandlers(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<List<GroupSummaryResponse>> Handle(GetGroupsQuery request, CancellationToken cancellationToken = default)
    {
        var groups = await VisibleGroupsAsync(cancellationToken);

        if (request.Year is not null)
        {
            groups = groups.Where(group => group.Year == request.Year);
        }

        if (request.GenerationId is not null)
        {
            groups = groups.Where(group => group.GenerationId == request.GenerationId);
        }

        if (request.IsActive is not null)
        {
            groups = groups.Where(group => group.IsActive == request.IsActive);
        }

        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var rows = await groups
            .OrderByDescending(group => group.Year).ThenBy(group => group.Generation.SortOrder).ThenBy(group => group.Name)
            .Select(group => new GroupRow(group.Id, group.Name, group.Year, group.GenerationId, group.Generation.Name, group.Generation.Profile, group.IsActive,
                group.Memberships.Count(membership => membership.EndedAt == null && !membership.Attendee.IsDeleted),
                group.Assignments.Select(assignment => new AssignmentRow(assignment.UserId, assignment.Responsibility)).ToList()))
            .ToListAsync(cancellationToken);

        return rows.Select(row => row.ToResponse(directory)).ToList();
    }

    public async Task<GroupDetailResponse> Handle(GetGroupByIdQuery request, CancellationToken cancellationToken = default)
    {
        var groups = await VisibleGroupsAsync(cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var row = await groups
            .Where(group => group.Id == request.Id)
            .Select(group => new GroupRow(group.Id, group.Name, group.Year, group.GenerationId, group.Generation.Name, group.Generation.Profile, group.IsActive,
                group.Memberships.Count(membership => membership.EndedAt == null && !membership.Attendee.IsDeleted),
                group.Assignments.Select(assignment => new AssignmentRow(assignment.UserId, assignment.Responsibility)).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Group), request.Id);

        var members = await _dbContext.GroupMemberships
            .Where(membership => membership.GroupId == request.Id && membership.EndedAt == null)
            .OrderBy(membership => membership.Attendee.FirstName).ThenBy(membership => membership.Attendee.LastName)
            .Select(membership => new GroupMemberResponse(
                membership.AttendeeId, membership.Attendee.FirstName + " " + membership.Attendee.LastName,
                membership.Attendee.IsServer, membership.Attendee.IsActive, membership.StartedAt))
            .ToListAsync(cancellationToken);

        var summary = row.ToResponse(directory);
        var leaderGenders = summary.Team.Where(member => member.Responsibility == GroupResponsibility.Leader).Select(member => member.Gender).ToList();
        var scope = await _access.GetScopeAsync(cancellationToken);

        return new GroupDetailResponse(
            summary,
            members,
            ExpectedLeadersComposition: leaderGenders.Contains(Gender.Male) && leaderGenders.Contains(Gender.Female),
            CanEdit: scope.IsDirector || scope.CoordinatedGroupIds.Contains(request.Id),
            CanManageCoordinators: scope.IsDirector);
    }

    public async Task<List<GroupHistoryEntry>> Handle(GetGroupHistoryQuery request, CancellationToken cancellationToken = default)
    {
        var groups = await VisibleGroupsAsync(cancellationToken);

        if (!await groups.AnyAsync(group => group.Id == request.Id, cancellationToken))
        {
            throw new NotFoundException(nameof(Group), request.Id);
        }

        return await _dbContext.GroupMemberships
            .Where(membership => membership.GroupId == request.Id)
            .OrderByDescending(membership => membership.StartedAt)
            .Select(membership => new GroupHistoryEntry(
                membership.AttendeeId, membership.Attendee.FirstName + " " + membership.Attendee.LastName, membership.StartedAt, membership.EndedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IQueryable<Group>> VisibleGroupsAsync(CancellationToken cancellationToken)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);

        if (scope.IsDirector)
        {
            return _dbContext.Groups;
        }

        var groupIds = scope.OperationalGroupIds;
        return _dbContext.Groups.Where(group => groupIds.Contains(group.Id));
    }

    private sealed record AssignmentRow(Guid UserId, GroupResponsibility Responsibility);

    private sealed record GroupRow(
        Guid Id, string Name, int Year, Guid GenerationId, string GenerationName, GenerationProfile Profile, bool IsActive,
        int AttendeeCount, List<AssignmentRow> Assignments)
    {
        public GroupSummaryResponse ToResponse(IReadOnlyDictionary<Guid, DirectoryUser> directory) => new(
            Id, Name, Year, new GenerationRef(GenerationId, GenerationName, Profile), IsActive, AttendeeCount,
            Assignments
                .OrderBy(assignment => assignment.Responsibility)
                .ThenBy(assignment => directory.NameOf(assignment.UserId))
                .Select(assignment => new TeamMember(
                    assignment.UserId, directory.NameOf(assignment.UserId), directory.GetValueOrDefault(assignment.UserId)?.Gender, assignment.Responsibility))
                .ToList());
    }
}

public sealed record TeamCandidate(Guid UserId, string FullName, Gender? Gender, UserRole Role);

/// <summary>
/// Cuentas activas que pueden integrar el equipo de un grupo (coordinadores, líderes y aprendices), para el editor
/// de equipo. Solo las consultan quienes editan equipos: el director y los coordinadores.
/// </summary>
public class GetTeamCandidatesQuery : IRequest<List<TeamCandidate>>;

public class GetTeamCandidatesQueryHandler : IRequestHandler<GetTeamCandidatesQuery, List<TeamCandidate>>
{
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetTeamCandidatesQueryHandler(MinistryAccess access, IUserDirectory userDirectory)
    {
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<List<TeamCandidate>> Handle(GetTeamCandidatesQuery request, CancellationToken cancellationToken = default)
    {
        if (!_access.IsDirector && _access.Role != UserRole.Coordinator)
        {
            throw new ForbiddenAccessException();
        }

        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        return directory.Values
            .Where(user => user.IsActive && user.Role is UserRole.Coordinator or UserRole.Leader or UserRole.Apprentice)
            .OrderBy(user => user.FullName)
            .Select(user => new TeamCandidate(user.Id, user.FullName, user.Gender, user.Role))
            .ToList();
    }
}

public class GetGroupsQueryValidator : AbstractValidator<GetGroupsQuery>
{
    public GetGroupsQueryValidator()
    {
        RuleFor(query => query.Year).InclusiveBetween(2000, 2200);
    }
}
