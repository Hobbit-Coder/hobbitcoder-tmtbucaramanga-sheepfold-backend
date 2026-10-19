using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;

/// <summary>Actualiza el evento: su creador, sus encargados o el director.</summary>
public class UpdateEventCommand : EventData, IRequest
{
    public Guid Id { get; set; }
}

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly EventOptionsService _options;
    private readonly EventAudienceResolver _audience;
    private readonly NotificationDispatcher _notifications;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public UpdateEventCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        EventOptionsService options,
        EventAudienceResolver audience,
        NotificationDispatcher notifications,
        IUserDirectory userDirectory,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _options = options;
        _audience = audience;
        _notifications = notifications;
        _userDirectory = userDirectory;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateEventCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await EventCommandHelpers.FindAdministrableAsync(_dbContext, _access, request.Id, cancellationToken);
        await EventCommandHelpers.ValidateAsync(_dbContext, _options, request, @event, cancellationToken);

        var before = EventCommandHelpers.Snapshot(@event);
        var previousManagers = @event.Managers.Select(manager => manager.UserId).ToList();

        @event.Title = request.Title;
        @event.EventTypeId = request.EventTypeId;
        @event.StartsAt = request.StartsAt.UtcDateTime;
        @event.EndsAt = request.EndsAt.UtcDateTime;
        @event.Location = request.Location;
        @event.Description = request.Description;
        @event.Audience = request.Audience;
        EventCommandHelpers.Apply(@event, request);

        _audit.Record(AuditModule.Events, AuditActions.Updated, @event.Id, @event.Title, before, EventCommandHelpers.Snapshot(@event));
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (@event.Status == EventStatus.Published)
        {
            await EventCommandHelpers.NotifyViewersAsync(_audience, _notifications, _userDirectory, @event, NotificationKind.EventChanged, "Evento actualizado",
                $"El evento {@event.Title} cambió. Revisa fecha, lugar y detalles.", cancellationToken);
        }

        await EventCommandHelpers.NotifyManagersAsync(_notifications, @event, request.ManagerIds.Except(previousManagers), cancellationToken);

        return Unit.Value;
    }
}

public class UpdateEventCommandValidator : EventDataValidator<UpdateEventCommand>
{
}
