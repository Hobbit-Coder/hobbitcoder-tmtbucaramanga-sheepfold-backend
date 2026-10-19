using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;

public class ChangeEventStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public EventStatus Status { get; set; }
}

public class ChangeEventStatusCommandHandler : IRequestHandler<ChangeEventStatusCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly EventAudienceResolver _audience;
    private readonly NotificationDispatcher _notifications;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public ChangeEventStatusCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        EventAudienceResolver audience,
        NotificationDispatcher notifications,
        IUserDirectory userDirectory,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audience = audience;
        _notifications = notifications;
        _userDirectory = userDirectory;
        _audit = audit;
    }

    public async Task<Unit> Handle(ChangeEventStatusCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await EventCommandHelpers.FindAdministrableAsync(_dbContext, _access, request.Id, cancellationToken);

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
            await EventCommandHelpers.NotifyViewersAsync(_audience, _notifications, _userDirectory, @event, NotificationKind.EventCancelled, "Evento cancelado",
                $"El evento {@event.Title} fue cancelado.", cancellationToken);
        }
        else if (request.Status == EventStatus.Published && before != EventStatus.Draft)
        {
            await EventCommandHelpers.NotifyViewersAsync(_audience, _notifications, _userDirectory, @event, NotificationKind.EventChanged, "Evento publicado de nuevo",
                $"El evento {@event.Title} vuelve a estar publicado.", cancellationToken);
        }

        return Unit.Value;
    }
}

public class ChangeEventStatusCommandValidator : AbstractValidator<ChangeEventStatusCommand>
{
    public ChangeEventStatusCommandValidator()
    {
        RuleFor(command => command.Status).IsInEnum();
    }
}
