using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Queries;

/// <summary>Lista administrativa de eventos visibles.</summary>
public class GetEventsQuery : IRequest<PagedResult<EventSummaryResponse>>
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Search { get; set; }
    public Guid? EventTypeId { get; set; }
    public Guid? GenerationId { get; set; }
    public Guid? GroupId { get; set; }
    public EventStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventSummaryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;

    public GetEventsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access)
    {
        _dbContext = dbContext;
        _access = access;
    }

    public async Task<PagedResult<EventSummaryResponse>> Handle(GetEventsQuery request, CancellationToken cancellationToken = default)
    {
        var events = await _access.VisibleEventsAsync(_dbContext.Events, cancellationToken);
        events = EventQueryHelpers.Filter(events, request.From, request.To, request.EventTypeId, request.GenerationId, request.GroupId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            events = events.Where(@event => @event.Title.ToUpper().Contains(search));
        }

        if (request.Status is not null)
        {
            events = events.Where(@event => @event.Status == request.Status);
        }

        return await events
            .OrderBy(@event => @event.StartsAt).ThenBy(@event => @event.Id)
            .Select(EventQueryHelpers.ToSummary)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    public GetEventsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Status).IsInEnum();
    }
}
