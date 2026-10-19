using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Queries;

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
            .Select(user => new TeamCandidate { UserId = user.Id, FullName = user.FullName, Gender = user.Gender, Role = user.Role })
            .ToList();
    }
}
