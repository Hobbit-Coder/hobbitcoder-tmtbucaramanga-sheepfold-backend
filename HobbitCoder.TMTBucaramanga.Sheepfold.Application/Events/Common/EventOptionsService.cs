using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

/// <summary>
/// Lo que el usuario puede elegir en un evento: el director cualquier público, generación o grupo
/// activo; el coordinador y el líder, uno o varios de los grupos activos que coordinan o lideran. Los
/// encargados se eligen entre las personas de ese mismo alcance.
/// </summary>
public sealed class EventOptionsService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public EventOptionsService(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<AudienceOptionsResponse> GetAsync(CancellationToken cancellationToken)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        if (scope.IsDirector)
        {
            var generations = await _dbContext.Generations.Where(item => item.IsActive).OrderBy(item => item.SortOrder)
                .Select(item => new NamedRef { Id = item.Id, Name = item.Name }).ToListAsync(cancellationToken);

            return new AudienceOptionsResponse
            {
                Audiences = Enum.GetValues<EventAudience>(),
                Generations = generations,
                Groups = await GroupsAsync(_dbContext.Groups.Where(group => group.IsActive), cancellationToken),
                Managers = directory.Values.Where(user => user.IsActive && user.IsMinistry).OrderBy(user => user.FullName)
                    .Select(user => new PersonRef { Id = user.Id, FullName = user.FullName, Role = user.Role }).ToList()
            };
        }

        var groupIds = scope.EventGroupIds;

        if (groupIds.Count == 0)
        {
            return new AudienceOptionsResponse { Audiences = [], Generations = [], Groups = [], Managers = [] };
        }

        var managerIds = await _dbContext.GroupAssignments.Where(assignment => groupIds.Contains(assignment.GroupId))
            .Select(assignment => assignment.UserId).Distinct().ToListAsync(cancellationToken);
        managerIds.Add(scope.UserId);

        return new AudienceOptionsResponse
        {
            Audiences = [EventAudience.Groups],
            Generations = [],
            Groups = await GroupsAsync(_dbContext.Groups.Where(group => groupIds.Contains(group.Id)), cancellationToken),
            Managers = managerIds.Distinct().Select(directory.GetValueOrDefault).OfType<DirectoryUser>().Where(user => user.IsActive)
                .OrderBy(user => user.FullName).Select(user => new PersonRef { Id = user.Id, FullName = user.FullName, Role = user.Role }).ToList()
        };
    }

    /// <summary>Puede crear eventos: el director, o un coordinador o líder con grupos activos.</summary>
    public async Task<bool> CanCreateAsync(CancellationToken cancellationToken)
    {
        var scope = await _access.GetScopeAsync(cancellationToken);

        return scope.IsDirector || scope.EventGroupIds.Count > 0;
    }

    /// <summary>
    /// Comprueba público, generaciones, grupos y encargados contra las opciones del usuario. Lo que ya tenía el
    /// evento y no cambia se acepta aunque esté fuera del alcance de quien edita (por ejemplo, un encargado).
    /// </summary>
    /// <returns>Mensaje del primer problema, o <c>null</c> si los datos son válidos.</returns>
    public async Task<string?> ValidateAsync(
        EventAudience audience, IReadOnlyCollection<Guid> generationIds, IReadOnlyCollection<Guid> groupIds, IReadOnlyCollection<Guid> managerIds,
        Event? existing, CancellationToken cancellationToken)
    {
        var options = await GetAsync(cancellationToken);
        var audienceUnchanged = existing is not null
            && existing.Audience == audience
            && existing.Generations.Select(item => item.GenerationId).ToHashSet().SetEquals(generationIds)
            && existing.Groups.Select(item => item.GroupId).ToHashSet().SetEquals(groupIds);

        if (!audienceUnchanged)
        {
            if (!options.Audiences.Contains(audience))
            {
                return "The selected audience is not available to you.";
            }

            if (generationIds.Any(id => options.Generations.All(item => item.Id != id)))
            {
                return "One or more generations are outside your scope.";
            }

            if (groupIds.Any(id => options.Groups.All(item => item.Id != id)))
            {
                return "One or more groups are outside your scope.";
            }
        }

        var previousManagers = existing?.Managers.Select(manager => manager.UserId).ToHashSet() ?? [];

        return managerIds.Any(id => !previousManagers.Contains(id) && options.Managers.All(item => item.Id != id))
            ? "One or more managers are outside your scope."
            : null;
    }

    private static async Task<IReadOnlyList<AudienceGroupRef>> GroupsAsync(IQueryable<Group> groups, CancellationToken cancellationToken) =>
        await groups.OrderBy(group => group.Generation.SortOrder).ThenBy(group => group.Name)
            .Select(group => new AudienceGroupRef { Id = group.Id, Name = group.Name, Year = group.Year, GenerationId = group.GenerationId, GenerationName = group.Generation.Name })
            .ToListAsync(cancellationToken);
}
