using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context;

/// <summary>
/// Responsabilidades de una cuenta en grupos, como dato administrativo mínimo para el editor de usuarios de Identity
/// (S10): sin asistentes ni métricas. Las consultan el administrador y el director.
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
            .Select(assignment => new MyGroupResponse(assignment.GroupId, assignment.Group.Name, assignment.Group.Year,
                assignment.Group.Generation.Name, assignment.Responsibility, assignment.Group.IsActive))
            .ToListAsync(cancellationToken);
    }
}
