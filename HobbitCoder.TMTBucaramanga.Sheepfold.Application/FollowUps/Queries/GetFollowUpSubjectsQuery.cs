using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Queries;

/// <summary>Personas a quienes el usuario puede hacer seguimiento: asistentes de sus grupos y usuarios inferiores.</summary>
public class GetFollowUpSubjectsQuery : IRequest<List<FollowUpSubject>>
{
    public string? Search { get; set; }
}

public class GetFollowUpSubjectsQueryHandler : IRequestHandler<GetFollowUpSubjectsQuery, List<FollowUpSubject>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetFollowUpSubjectsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<List<FollowUpSubject>> Handle(GetFollowUpSubjectsQuery request, CancellationToken cancellationToken = default)
    {
        if (!_access.CanFollowUp)
        {
            throw new ForbiddenAccessException();
        }

        var scope = await _access.GetScopeAsync(cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var search = request.Search?.Trim().ToUpperInvariant() ?? string.Empty;

        var attendees = _dbContext.Attendees.Where(attendee => attendee.IsActive);
        if (!scope.IsDirector)
        {
            var groupIds = scope.ActiveManagedGroupIds;
            attendees = attendees.Where(attendee => attendee.Memberships.Any(membership => membership.EndedAt == null && groupIds.Contains(membership.GroupId)));
        }

        var attendeeSubjects = await attendees
            .Where(attendee => search == string.Empty || (attendee.FirstName + " " + attendee.LastName).ToUpper().Contains(search))
            .OrderBy(attendee => attendee.FirstName).ThenBy(attendee => attendee.LastName)
            .Take(50)
            .Select(attendee => new FollowUpSubject
            {
                Type = PersonType.Attendee,
                Id = attendee.Id,
                FullName = attendee.FirstName + " " + attendee.LastName,
                Context = attendee.Memberships.Where(membership => membership.EndedAt == null)
                    .Select(membership => membership.Group.Generation.Name + " · " + membership.Group.Name).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var inferior = await _access.InferiorUserIdsAsync(directory, cancellationToken);
        var userSubjects = inferior.Select(directory.GetValueOrDefault).OfType<DirectoryUser>()
            .Where(user => user.IsActive && user.FullName.ToUpperInvariant().Contains(search))
            .OrderBy(user => user.FullName)
            .Take(50)
            .Select(user => new FollowUpSubject { Type = PersonType.User, Id = user.Id, FullName = user.FullName, Context = RoleLabels.Of(user.Role) });

        return [.. userSubjects, .. attendeeSubjects];
    }
}
