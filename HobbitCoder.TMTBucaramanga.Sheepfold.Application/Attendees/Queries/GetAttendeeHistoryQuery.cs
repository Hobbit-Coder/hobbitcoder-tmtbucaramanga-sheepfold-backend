using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Queries;

/// <summary>Historial de grupos y generaciones, cada uno con su año y periodo.</summary>
public class GetAttendeeHistoryQuery : IRequest<List<MembershipHistoryItem>>
{
    public Guid Id { get; set; }
}

public class GetAttendeeHistoryQueryHandler : IRequestHandler<GetAttendeeHistoryQuery, List<MembershipHistoryItem>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetAttendeeHistoryQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<List<MembershipHistoryItem>> Handle(GetAttendeeHistoryQuery request, CancellationToken cancellationToken = default)
    {
        var attendee = await _access.FindAttendeeInScopeAsync(request.Id, cancellationToken);

        return await _dbContext.GroupMemberships
            .IgnoreQueryFilters()
            .Where(membership => membership.AttendeeId == attendee.Id)
            .OrderByDescending(membership => membership.StartedAt)
            .Select(membership => new MembershipHistoryItem
            {
                Group = new GroupRef
                {
                    Id = membership.GroupId,
                    Name = membership.Group.Name,
                    Year = membership.Group.Year,
                    GenerationId = membership.Group.GenerationId,
                    GenerationName = membership.Group.Generation.Name,
                    GenerationProfile = membership.Group.Generation.Profile
                },
                StartedAt = membership.StartedAt,
                EndedAt = membership.EndedAt
            })
            .ToListAsync(cancellationToken);
    }
}
