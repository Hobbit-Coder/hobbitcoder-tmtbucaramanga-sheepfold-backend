using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Queries;

public class GetNotificationOptionsQuery : IRequest<NotificationOptionsDto>
{
}

public class GetNotificationOptionsQueryHandler : IRequestHandler<GetNotificationOptionsQuery, NotificationOptionsDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetNotificationOptionsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<NotificationOptionsDto> Handle(GetNotificationOptionsQuery request, CancellationToken cancellationToken = default)
    {
        SettingsHelpers.EnsureAdminOrDirector(_access);

        return await SettingsHelpers.LoadOptionsAsync(_dbContext, cancellationToken);
    }
}
