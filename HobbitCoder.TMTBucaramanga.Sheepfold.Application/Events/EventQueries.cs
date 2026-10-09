using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events;

/// <summary>Lista administrativa de eventos visibles (S03, Calendario → Agenda → Eventos).</summary>
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

public class GetEventQuery : IRequest<EventDetailResponse>
{
    public Guid Id { get; set; }
}

public class GetEventOptionsQuery : IRequest<AudienceOptionsResponse>
{
}

public class EventQueryHandlers :
    IRequestHandler<GetEventsQuery, PagedResult<EventSummaryResponse>>,
    IRequestHandler<GetEventQuery, EventDetailResponse>,
    IRequestHandler<GetEventOptionsQuery, AudienceOptionsResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;
    private readonly EventOptionsService _options;

    public EventQueryHandlers(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory, EventOptionsService options)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
        _options = options;
    }

    /// <summary>Proyección común del resumen de un evento.</summary>
    public static readonly Expression<Func<Event, EventSummaryResponse>> ToSummary = @event => new EventSummaryResponse(
        @event.Id,
        @event.Title,
        new NamedRef(@event.EventTypeId, @event.EventType.Name),
        @event.StartsAt,
        @event.EndsAt,
        @event.Location,
        @event.Audience,
        @event.Generations.OrderBy(item => item.Generation.SortOrder).Select(item => new NamedRef(item.GenerationId, item.Generation.Name)).ToList(),
        @event.Groups.Select(item => new AudienceGroupRef(item.GroupId, item.Group.Name, item.Group.Year, item.Group.GenerationId, item.Group.Generation.Name)).ToList(),
        @event.Status,
        @event.RequiresAttendance);

    public async Task<PagedResult<EventSummaryResponse>> Handle(GetEventsQuery request, CancellationToken cancellationToken = default)
    {
        var events = await _access.VisibleEventsAsync(_dbContext.Events, cancellationToken);
        events = Filter(events, request.From, request.To, request.EventTypeId, request.GenerationId, request.GroupId);

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
            .Select(ToSummary)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }

    public async Task<EventDetailResponse> Handle(GetEventQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.Id, cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var summary = await _dbContext.Events.Where(item => item.Id == @event.Id).Select(ToSummary).FirstAsync(cancellationToken);
        var attachments = await _dbContext.EventAttachments
            .Where(attachment => attachment.EventId == @event.Id)
            .OrderBy(attachment => attachment.CreatedDate)
            .Select(attachment => new { attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedDate, attachment.CreatedBy })
            .ToListAsync(cancellationToken);

        var canAdminister = _access.CanAdministerEvent(@event);
        var scope = await _access.GetScopeAsync(cancellationToken);
        var attendanceOpen = @event.RequiresAttendance && @event.Status is EventStatus.Published or EventStatus.Finished;

        return new EventDetailResponse(
            summary,
            @event.Description,
            Person(directory, @event.CreatedBy),
            @event.Managers.Select(manager => Person(directory, manager.UserId.ToString())).OrderBy(person => person.FullName).ToList(),
            attachments.Select(item => new FileResponse(item.Id, item.FileName, item.ContentType, item.SizeBytes, item.CreatedDate, directory.NameOf(item.CreatedBy))).ToList(),
            canAdminister ? EventTransitions.From(@event.Status) : [],
            new EventCapabilities(
                Edit: canAdminister,
                ChangeStatus: canAdminister,
                Retire: canAdminister,
                Attendance: attendanceOpen && (canAdminister || scope.OperationalGroupIds.Count > 0),
                Comment: true));
    }

    public Task<AudienceOptionsResponse> Handle(GetEventOptionsQuery request, CancellationToken cancellationToken = default) =>
        _options.GetAsync(cancellationToken);

    /// <summary>
    /// Filtros de fecha, tipo, generación y grupo (§14). Se aplican sobre los eventos visibles: nunca amplían el
    /// alcance. Un evento de grupos coincide con una generación si alguno de sus grupos es de ella.
    /// </summary>
    public static IQueryable<Event> Filter(IQueryable<Event> events, DateTimeOffset? from, DateTimeOffset? to, Guid? eventTypeId, Guid? generationId, Guid? groupId)
    {
        if (from is not null)
        {
            var fromUtc = from.Value.UtcDateTime;
            events = events.Where(@event => @event.EndsAt >= fromUtc);
        }

        if (to is not null)
        {
            var toUtc = to.Value.UtcDateTime;
            events = events.Where(@event => @event.StartsAt < toUtc);
        }

        if (eventTypeId is not null)
        {
            events = events.Where(@event => @event.EventTypeId == eventTypeId);
        }

        if (generationId is not null)
        {
            events = events.Where(@event => @event.Generations.Any(item => item.GenerationId == generationId)
                || @event.Groups.Any(item => item.Group.GenerationId == generationId));
        }

        if (groupId is not null)
        {
            events = events.Where(@event => @event.Groups.Any(item => item.GroupId == groupId));
        }

        return events;
    }

    private static PersonRef Person(IReadOnlyDictionary<Guid, DirectoryUser> directory, string userId) =>
        Guid.TryParse(userId, out var id) && directory.TryGetValue(id, out var user)
            ? new PersonRef(user.Id, user.FullName, user.Role)
            : new PersonRef(Guid.Empty, directory.NameOf(userId), null);
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
