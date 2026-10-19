using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Queries;

/// <summary>
/// Grupos visibles: el director, todos; los demás, los grupos donde tienen una responsabilidad (también
/// los de años anteriores, para la consulta histórica).
/// </summary>
public class GetGroupsQuery : IRequest<List<GroupSummaryResponse>>
{
    public int? Year { get; set; }
    public Guid? GenerationId { get; set; }
    public bool? IsActive { get; set; }
}

public class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, List<GroupSummaryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetGroupsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<List<GroupSummaryResponse>> Handle(GetGroupsQuery request, CancellationToken cancellationToken = default)
    {
        var groups = await GroupQueryHelpers.VisibleGroupsAsync(_dbContext, _access, cancellationToken);

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
            .ToListAsync(cancellationToken);

        return rows.Select(row => row.ToResponse(directory)).ToList();
    }
}

public class GetGroupsQueryValidator : AbstractValidator<GetGroupsQuery>
{
    public GetGroupsQueryValidator()
    {
        RuleFor(query => query.Year).InclusiveBetween(2000, 2200);
    }
}
