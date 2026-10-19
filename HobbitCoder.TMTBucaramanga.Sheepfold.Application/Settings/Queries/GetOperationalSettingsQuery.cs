using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Queries;

/// <summary>La consultan el administrador y el director ("solo consulta" para el director).</summary>
public class GetOperationalSettingsQuery : IRequest<OperationalSettingsDto>
{
}

public class GetOperationalSettingsQueryHandler : IRequestHandler<GetOperationalSettingsQuery, OperationalSettingsDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetOperationalSettingsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<OperationalSettingsDto> Handle(GetOperationalSettingsQuery request, CancellationToken cancellationToken = default)
    {
        SettingsHelpers.EnsureAdminOrDirector(_access);

        return SettingsHelpers.ToDto(await SettingsHelpers.LoadSettingsAsync(_dbContext, cancellationToken));
    }
}
