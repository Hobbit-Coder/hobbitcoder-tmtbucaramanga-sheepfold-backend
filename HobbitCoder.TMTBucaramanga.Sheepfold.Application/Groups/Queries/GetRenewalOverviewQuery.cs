using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Queries;

/// <summary>
/// Organización de los grupos de un año: asistentes activos cuyo grupo vigente es de un año
/// anterior y deben quedar en un grupo del año nuevo. Los grupos del año se consultan con <see cref="GetGroupsQuery"/>.
/// </summary>
public class GetRenewalOverviewQuery : IRequest<RenewalOverviewResponse>
{
    public int Year { get; set; }
}

public class GetRenewalOverviewQueryHandler : IRequestHandler<GetRenewalOverviewQuery, RenewalOverviewResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetRenewalOverviewQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<RenewalOverviewResponse> Handle(GetRenewalOverviewQuery request, CancellationToken cancellationToken = default)
    {
        _access.EnsureDirector();

        var pending = await _dbContext.GroupMemberships
            .Where(membership => membership.EndedAt == null && membership.Attendee.IsActive && membership.Group.Year < request.Year)
            .OrderBy(membership => membership.Group.Generation.SortOrder).ThenBy(membership => membership.Attendee.FirstName)
            .Select(membership => new PendingReassignment
            {
                AttendeeId = membership.AttendeeId,
                FullName = membership.Attendee.FirstName + " " + membership.Attendee.LastName,
                CurrentGroupId = membership.GroupId,
                CurrentGroup = membership.Group.Name,
                CurrentYear = membership.Group.Year,
                GenerationId = membership.Group.GenerationId,
                GenerationName = membership.Group.Generation.Name
            })
            .ToListAsync(cancellationToken);

        return new RenewalOverviewResponse { Year = request.Year, PendingAttendees = pending };
    }
}
