using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Queries;

/// <summary>
/// Responsabilidades de una cuenta en grupos, como dato administrativo mínimo para el editor de usuarios de Identity
///: sin asistentes ni métricas. Las consultan el administrador y el director.
/// </summary>
public class GetUserResponsibilitiesQuery : IRequest<List<MyGroupResponse>>
{
    public Guid UserId { get; set; }
}

public class GetUserResponsibilitiesQueryHandler : IRequestHandler<GetUserResponsibilitiesQuery, List<MyGroupResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetUserResponsibilitiesQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public Task<List<MyGroupResponse>> Handle(GetUserResponsibilitiesQuery request, CancellationToken cancellationToken = default)
    {
        if (!_access.IsAdmin && !_access.IsDirector)
        {
            throw new ForbiddenAccessException();
        }

        return _dbContext.GroupAssignments
            .Where(assignment => assignment.UserId == request.UserId)
            .OrderByDescending(assignment => assignment.Group.IsActive).ThenByDescending(assignment => assignment.Group.Year)
            .Select(assignment => new MyGroupResponse
            {
                GroupId = assignment.GroupId,
                Name = assignment.Group.Name,
                Year = assignment.Group.Year,
                GenerationName = assignment.Group.Generation.Name,
                Responsibility = assignment.Responsibility,
                IsActive = assignment.Group.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
