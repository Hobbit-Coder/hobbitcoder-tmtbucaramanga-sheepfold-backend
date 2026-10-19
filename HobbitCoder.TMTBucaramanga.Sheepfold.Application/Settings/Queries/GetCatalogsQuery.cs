using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Queries;

/// <summary>
/// Todas las listas. Las consultan el administrador (para administrarlas) y los usuarios ministeriales (para
/// operar); no contienen información ministerial. Por defecto solo los valores activos.
/// </summary>
public class GetCatalogsQuery : IRequest<CatalogsResponse>
{
    public bool IncludeInactive { get; set; }
}

public class GetCatalogsQueryHandler : IRequestHandler<GetCatalogsQuery, CatalogsResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetCatalogsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    /// <summary>
    ///     Atiende &lt;see cref="GetCatalogsQuery"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Solo el administrador ve los valores retirados (para restaurarlos).
    /// </remarks>
    public async Task<CatalogsResponse> Handle(GetCatalogsQuery request, CancellationToken cancellationToken = default)
    {
        if (!_access.IsAdmin && !_access.IsMinistry)
        {
            throw new ForbiddenAccessException();
        }

        var includeInactive = request.IncludeInactive && _access.IsAdmin;

        var generations = await _dbContext.Generations
            .Where(item => includeInactive || item.IsActive)
            .OrderBy(item => item.SortOrder).ThenBy(item => item.Name)
            .Select(item => new CatalogItemResponse { Id = item.Id, Name = item.Name, IsActive = item.IsActive, Profile = item.Profile, SortOrder = item.SortOrder })
            .ToListAsync(cancellationToken);

        return new CatalogsResponse
        {
            Generations = generations,
            EventTypes = await ListAsync(_dbContext.EventTypes, includeInactive, cancellationToken),
            FollowUpTopics = await ListAsync(_dbContext.FollowUpTopics, includeInactive, cancellationToken),
            Courses = await ListAsync(_dbContext.Courses, includeInactive, cancellationToken)
        };
    }

    private static async Task<IReadOnlyList<CatalogItemResponse>> ListAsync<T>(IQueryable<T> items, bool includeInactive, CancellationToken cancellationToken)
        where T : CatalogItem =>
        await items
            .Where(item => includeInactive || item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => new CatalogItemResponse { Id = item.Id, Name = item.Name, IsActive = item.IsActive, Profile = null, SortOrder = null })
            .ToListAsync(cancellationToken);
}
