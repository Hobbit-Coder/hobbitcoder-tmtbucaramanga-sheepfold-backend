using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;

/// <summary>
/// Crea un evento como borrador o publicado. El director, o un coordinador o líder para sus grupos;
/// nunca el aprendiz ni el administrador.
/// </summary>
public class CreateEventCommand : EventData, IRequest<Guid>
{
    public bool Publish { get; set; }
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly EventOptionsService _options;
    private readonly NotificationDispatcher _notifications;
    private readonly AuditTrail _audit;

    public CreateEventCommandHandler(
        IApplicationDbContext dbContext,
        EventOptionsService options,
        NotificationDispatcher notifications,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _options = options;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken = default)
    {
        if (!await _options.CanCreateAsync(cancellationToken))
        {
            throw new ForbiddenAccessException();
        }

        await EventCommandHelpers.ValidateAsync(_dbContext, _options, request, existing: null, cancellationToken);

        var @event = new Event
        {
            Title = request.Title,
            EventTypeId = request.EventTypeId,
            StartsAt = request.StartsAt.UtcDateTime,
            EndsAt = request.EndsAt.UtcDateTime,
            Location = request.Location,
            Description = request.Description,
            Audience = request.Audience,
            Status = request.Publish ? EventStatus.Published : EventStatus.Draft
        };
        EventCommandHelpers.Apply(@event, request);
        _dbContext.Events.Add(@event);

        _audit.Record(AuditModule.Events, AuditActions.Created, @event.Id, @event.Title, null, EventCommandHelpers.Snapshot(@event));
        await _dbContext.SaveChangesAsync(cancellationToken);

        await EventCommandHelpers.NotifyManagersAsync(_notifications, @event, request.ManagerIds, cancellationToken);

        return @event.Id;
    }
}

public class CreateEventCommandValidator : EventDataValidator<CreateEventCommand>
{
}
