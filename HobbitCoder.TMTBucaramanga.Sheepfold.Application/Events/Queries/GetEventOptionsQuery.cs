using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Queries;

public class GetEventOptionsQuery : IRequest<AudienceOptionsResponse>
{
}

public class GetEventOptionsQueryHandler : IRequestHandler<GetEventOptionsQuery, AudienceOptionsResponse>
{
    private readonly EventOptionsService _options;

    public GetEventOptionsQueryHandler(EventOptionsService options)
    {
        _options = options;
    }

    public Task<AudienceOptionsResponse> Handle(GetEventOptionsQuery request, CancellationToken cancellationToken = default) =>
        _options.GetAsync(cancellationToken);
}
