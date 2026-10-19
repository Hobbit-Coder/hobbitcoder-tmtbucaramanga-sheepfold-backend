using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Queries;

/// <summary>
/// Contexto del usuario para la interfaz: rol, capacidades y responsabilidades. Sirve para mostrar solo
/// destinos y acciones permitidos; cada endpoint vuelve a autorizar en el servidor.
/// </summary>
public class GetMyContextQuery : IRequest<MyContextResponse>
{
}

public class GetMyContextQueryHandler : IRequestHandler<GetMyContextQuery, MyContextResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetMyContextQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<MyContextResponse> Handle(GetMyContextQuery request, CancellationToken cancellationToken = default)
    {
        var userId = _access.UserId;
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var role = _access.Role;

        var groups = !_access.IsMinistry ? [] : await _dbContext.GroupAssignments
            .Where(assignment => assignment.UserId == userId)
            .OrderByDescending(assignment => assignment.Group.IsActive).ThenByDescending(assignment => assignment.Group.Year).ThenBy(assignment => assignment.Group.Name)
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

        var isDirector = _access.IsDirector;
        var managesGroups = groups.Any(group => group.IsActive && group.Responsibility is GroupResponsibility.Coordinator or GroupResponsibility.Leader);

        var capabilities = new MyCapabilities
        {
            Ministry = _access.IsMinistry,
            CreateEvents = isDirector || (role is UserRole.Coordinator or UserRole.Leader && managesGroups),
            FollowUps = _access.CanFollowUp,
            RegisterAttendees = _access.CanRegisterAttendees,
            Reports = _access.IsMinistry,
            Audit = isDirector,
            ManageConfiguration = _access.IsAdmin,
            ViewConfiguration = _access.IsAdmin || isDirector,
            RetiredRecords = _access.IsAdmin || isDirector,
            ManageGroups = isDirector || groups.Any(group => group.IsActive && group.Responsibility == GroupResponsibility.Coordinator)
        };

        return new MyContextResponse { UserId = userId, FullName = directory.NameOf(userId), Role = role, Capabilities = capabilities, Groups = groups };
    }
}
