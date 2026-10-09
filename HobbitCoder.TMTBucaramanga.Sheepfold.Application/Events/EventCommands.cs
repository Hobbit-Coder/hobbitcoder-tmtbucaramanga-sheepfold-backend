using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events;

/// <summary>Datos del evento (§15). El creador y el estado no se capturan: los define el servidor.</summary>
public abstract class EventData
{
    public required string Title { get; set; }
    public Guid EventTypeId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public required string Location { get; set; }
    public required string Description { get; set; }
    public EventAudience Audience { get; set; }
    public List<Guid> GenerationIds { get; set; } = [];
    public List<Guid> GroupIds { get; set; } = [];
    public bool RequiresAttendance { get; set; }
    public List<Guid> ManagerIds { get; set; } = [];
}

/// <summary>
/// Crea un evento como borrador o publicado (HU-13). El director, o un coordinador o líder para sus grupos;
/// nunca el aprendiz ni el administrador.
/// </summary>
public class CreateEventCommand : EventData, IRequest<Guid>
{
    public bool Publish { get; set; }
}

/// <summary>Actualiza el evento: su creador, sus encargados o el director (HU-15).</summary>
public class UpdateEventCommand : EventData, IRequest
{
    public Guid Id { get; set; }
}

public class ChangeEventStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public EventStatus Status { get; set; }
}

/// <summary>Retira el evento de las consultas habituales (§26); conserva asistencia, conversación y archivos.</summary>
public class RetireEventCommand : IRequest
{
    public Guid Id { get; set; }
}

public class EventCommandHandlers :
    IRequestHandler<CreateEventCommand, Guid>,
    IRequestHandler<UpdateEventCommand, Unit>,
    IRequestHandler<ChangeEventStatusCommand, Unit>,
    IRequestHandler<RetireEventCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly EventOptionsService _options;
    private readonly EventAudienceResolver _audience;
    private readonly NotificationDispatcher _notifications;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public EventCommandHandlers(
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

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken = default)
    {
        if (!await _options.CanCreateAsync(cancellationToken))
        {
            throw new ForbiddenAccessException();
        }

        await ValidateAsync(request, existing: null, cancellationToken);

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
        Apply(@event, request);
        _dbContext.Events.Add(@event);

        _audit.Record(AuditModule.Events, AuditActions.Created, @event.Id, @event.Title, null, Snapshot(@event));
        await _dbContext.SaveChangesAsync(cancellationToken);

        await NotifyManagersAsync(@event, request.ManagerIds, cancellationToken);

        return @event.Id;
    }

    public async Task<Unit> Handle(UpdateEventCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await FindAdministrableAsync(request.Id, cancellationToken);
        await ValidateAsync(request, @event, cancellationToken);

        var before = Snapshot(@event);
        var previousManagers = @event.Managers.Select(manager => manager.UserId).ToList();

        @event.Title = request.Title;
        @event.EventTypeId = request.EventTypeId;
        @event.StartsAt = request.StartsAt.UtcDateTime;
        @event.EndsAt = request.EndsAt.UtcDateTime;
        @event.Location = request.Location;
        @event.Description = request.Description;
        @event.Audience = request.Audience;
        Apply(@event, request);

        _audit.Record(AuditModule.Events, AuditActions.Updated, @event.Id, @event.Title, before, Snapshot(@event));
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (@event.Status == EventStatus.Published)
        {
            await NotifyViewersAsync(@event, NotificationKind.EventChanged, "Evento actualizado",
                $"El evento {@event.Title} cambió. Revisa fecha, lugar y detalles.", cancellationToken);
        }

        await NotifyManagersAsync(@event, request.ManagerIds.Except(previousManagers), cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(ChangeEventStatusCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await FindAdministrableAsync(request.Id, cancellationToken);

        if (!EventTransitions.From(@event.Status).Contains(request.Status))
        {
            throw new ConflictException($"The event cannot change from {@event.Status} to {request.Status}.");
        }

        var before = @event.Status;
        @event.Status = request.Status;
        _audit.Record(AuditModule.Events, AuditActions.StatusChanged, @event.Id, @event.Title, new { Status = before }, new { @event.Status });
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.Status == EventStatus.Cancelled && before == EventStatus.Published)
        {
            await NotifyViewersAsync(@event, NotificationKind.EventCancelled, "Evento cancelado",
                $"El evento {@event.Title} fue cancelado.", cancellationToken);
        }
        else if (request.Status == EventStatus.Published && before != EventStatus.Draft)
        {
            await NotifyViewersAsync(@event, NotificationKind.EventChanged, "Evento publicado de nuevo",
                $"El evento {@event.Title} vuelve a estar publicado.", cancellationToken);
        }

        return Unit.Value;
    }

    public async Task<Unit> Handle(RetireEventCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await FindAdministrableAsync(request.Id, cancellationToken);

        _dbContext.Events.Remove(@event);
        _audit.Record(AuditModule.Events, AuditActions.Retired, @event.Id, @event.Title, Snapshot(@event), null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task<Event> FindAdministrableAsync(Guid id, CancellationToken cancellationToken)
    {
        var visible = await _access.VisibleEventsAsync(_dbContext.Events, cancellationToken);
        var @event = await visible
            .Include(item => item.Managers)
            .Include(item => item.Generations)
            .Include(item => item.Groups)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), id);

        if (!_access.CanAdministerEvent(@event))
        {
            throw new ForbiddenAccessException();
        }

        return @event;
    }

    private async Task ValidateAsync(EventData request, Event? existing, CancellationToken cancellationToken)
    {
        // Un tipo retirado solo se acepta si el evento ya lo tenía.
        var currentTypeId = existing?.EventTypeId;

        if (!await _dbContext.EventTypes.AnyAsync(type => type.Id == request.EventTypeId && (type.IsActive || type.Id == currentTypeId), cancellationToken))
        {
            throw ValidationException.For(nameof(request.EventTypeId), "Select an event type.");
        }

        var problem = await _options.ValidateAsync(request.Audience, request.GenerationIds, request.GroupIds, request.ManagerIds, existing, cancellationToken);

        if (problem is not null)
        {
            throw ValidationException.For(nameof(request.Audience), problem);
        }
    }

    /// <summary>
    /// Sincroniza las asociaciones con lo pedido: conserva las que siguen, quita las que sobran y agrega las
    /// nuevas. Solo se guardan las generaciones o los grupos que corresponden al público elegido (§17): "Por
    /// grupos" es el público de grupos.
    /// </summary>
    private static void Apply(Event @event, EventData request)
    {
        @event.RequiresAttendance = request.RequiresAttendance;

        var generationIds = request.Audience == EventAudience.Generations ? request.GenerationIds.ToHashSet() : [];
        var groupIds = request.Audience == EventAudience.Groups ? request.GroupIds.ToHashSet() : [];
        var managerIds = request.ManagerIds.ToHashSet();

        Sync(@event.Generations, generationIds, item => item.GenerationId, id => new EventGeneration { EventId = @event.Id, GenerationId = id });
        Sync(@event.Groups, groupIds, item => item.GroupId, id => new EventGroup { EventId = @event.Id, GroupId = id });
        Sync(@event.Managers, managerIds, item => item.UserId, id => new EventManager { EventId = @event.Id, UserId = id });
    }

    private static void Sync<T>(ICollection<T> items, HashSet<Guid> wanted, Func<T, Guid> key, Func<Guid, T> create)
    {
        foreach (var item in items.Where(item => !wanted.Contains(key(item))).ToList())
        {
            items.Remove(item);
        }

        foreach (var id in wanted.Where(id => items.All(item => key(item) != id)))
        {
            items.Add(create(id));
        }
    }

    private async Task NotifyViewersAsync(Event @event, NotificationKind kind, string title, string message, CancellationToken cancellationToken)
    {
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        var viewers = await _audience.ViewerUserIdsAsync(@event, directory, cancellationToken);

        await _notifications.NotifyAsync(viewers, kind, title, message, eventId: @event.Id, cancellationToken: cancellationToken);
    }

    private Task NotifyManagersAsync(Event @event, IEnumerable<Guid> managerIds, CancellationToken cancellationToken) =>
        _notifications.NotifyAsync(managerIds, NotificationKind.EventResponsibility, $"Eres encargado de {@event.Title}",
            $"Se te asignó como encargado del evento {@event.Title}.", eventId: @event.Id, cancellationToken: cancellationToken);

    private static object Snapshot(Event @event) => new
    {
        @event.Title,
        @event.EventTypeId,
        @event.StartsAt,
        @event.EndsAt,
        @event.Location,
        @event.Description,
        @event.Audience,
        Generations = @event.Generations.Select(item => item.GenerationId).ToList(),
        Groups = @event.Groups.Select(item => item.GroupId).ToList(),
        Managers = @event.Managers.Select(item => item.UserId).ToList(),
        @event.RequiresAttendance,
        @event.Status
    };
}

public class EventDataValidator<T> : AbstractValidator<T> where T : EventData
{
    public EventDataValidator()
    {
        RuleFor(command => command.Title).NotEmpty().WithMessage("Title is required").MaximumLength(150);
        RuleFor(command => command.EventTypeId).NotEmpty().WithMessage("Select an event type.");
        RuleFor(command => command.Location).NotEmpty().WithMessage("Location is required").MaximumLength(150);
        RuleFor(command => command.Description).NotEmpty().WithMessage("Description is required").MaximumLength(4000);
        RuleFor(command => command.Audience).IsInEnum();
        RuleFor(command => command.EndsAt).GreaterThanOrEqualTo(command => command.StartsAt).WithMessage("The event must end after it starts.");
        RuleFor(command => command.ManagerIds).NotEmpty().WithMessage("Assign at least one manager.");
        RuleFor(command => command.GenerationIds).NotEmpty().When(command => command.Audience == EventAudience.Generations)
            .WithMessage("Select at least one generation.");
        RuleFor(command => command.GroupIds).NotEmpty().When(command => command.Audience == EventAudience.Groups)
            .WithMessage("Select at least one group.");
    }
}

public class CreateEventCommandValidator : EventDataValidator<CreateEventCommand>
{
}

public class UpdateEventCommandValidator : EventDataValidator<UpdateEventCommand>
{
}

public class ChangeEventStatusCommandValidator : AbstractValidator<ChangeEventStatusCommand>
{
    public ChangeEventStatusCommandValidator()
    {
        RuleFor(command => command.Status).IsInEnum();
    }
}
