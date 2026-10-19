using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Queries;

/// <summary>Pertenencias actuales y anteriores del grupo (historia).</summary>
public class GetGroupHistoryQuery : IRequest<List<GroupHistoryEntry>>
{
    public Guid Id { get; set; }
}

public class GetGroupHistoryQueryHandler : IRequestHandler<GetGroupHistoryQuery, List<GroupHistoryEntry>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetGroupHistoryQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<List<GroupHistoryEntry>> Handle(GetGroupHistoryQuery request, CancellationToken cancellationToken = default)
    {
        var groups = await GroupQueryHelpers.VisibleGroupsAsync(_dbContext, _access, cancellationToken);

        if (!await groups.AnyAsync(group => group.Id == request.Id, cancellationToken))
        {
            throw new NotFoundException(nameof(Group), request.Id);
        }

        return await _dbContext.GroupMemberships
            .Where(membership => membership.GroupId == request.Id)
            .OrderByDescending(membership => membership.StartedAt)
            .Select(membership => new GroupHistoryEntry
            {
                AttendeeId = membership.AttendeeId,
                FullName = membership.Attendee.FirstName + " " + membership.Attendee.LastName,
                StartedAt = membership.StartedAt,
                EndedAt = membership.EndedAt
            })
            .ToListAsync(cancellationToken);
    }
}
