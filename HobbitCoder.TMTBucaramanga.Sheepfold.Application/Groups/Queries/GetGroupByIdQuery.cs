using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Queries;

public class GetGroupByIdQuery : IRequest<GroupDetailResponse>
{
    public Guid Id { get; set; }
}

public class GetGroupByIdQueryHandler : IRequestHandler<GetGroupByIdQuery, GroupDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetGroupByIdQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<GroupDetailResponse> Handle(GetGroupByIdQuery request, CancellationToken cancellationToken = default)
    {
        var groups = await GroupQueryHelpers.VisibleGroupsAsync(_dbContext, _access, cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var row = await groups
            .Where(group => group.Id == request.Id)
            .Select(group => new GroupRow
            {
                Id = group.Id,
                Name = group.Name,
                Year = group.Year,
                GenerationId = group.GenerationId,
                GenerationName = group.Generation.Name,
                Profile = group.Generation.Profile,
                IsActive = group.IsActive,
                AttendeeCount = group.Memberships.Count(membership => membership.EndedAt == null && !membership.Attendee.IsDeleted),
                Assignments = group.Assignments.Select(assignment => new AssignmentRow { UserId = assignment.UserId, Responsibility = assignment.Responsibility }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Group), request.Id);

        var members = await _dbContext.GroupMemberships
            .Where(membership => membership.GroupId == request.Id && membership.EndedAt == null)
            .OrderBy(membership => membership.Attendee.FirstName).ThenBy(membership => membership.Attendee.LastName)
            .Select(membership => new GroupMemberResponse
            {
                AttendeeId = membership.AttendeeId,
                FullName = membership.Attendee.FirstName + " " + membership.Attendee.LastName,
                IsServer = membership.Attendee.IsServer,
                IsActive = membership.Attendee.IsActive,
                Since = membership.StartedAt
            })
            .ToListAsync(cancellationToken);

        var summary = row.ToResponse(directory);
        var leaderGenders = summary.Team.Where(member => member.Responsibility == GroupResponsibility.Leader).Select(member => member.Gender).ToList();
        var scope = await _access.GetScopeAsync(cancellationToken);

        return new GroupDetailResponse
        {
            Group = summary,
            Members = members,
            ExpectedLeadersComposition = leaderGenders.Contains(Gender.Male) && leaderGenders.Contains(Gender.Female),
            CanEdit = scope.IsDirector || scope.CoordinatedGroupIds.Contains(request.Id),
            CanManageCoordinators = scope.IsDirector
        };
    }
}
