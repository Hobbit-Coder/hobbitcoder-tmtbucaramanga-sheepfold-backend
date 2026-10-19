using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Queries;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Commands;

/// <summary>
/// Elimina definitivamente un registro previamente retirado. La interfaz muestra antes su impacto y exige
/// confirmación. Los archivos de un evento se borran del almacenamiento después de confirmar el borrado en la base.
/// </summary>
public class DeleteRetiredRecordCommand : IRequest
{
    public RetiredType Type { get; set; }
    public Guid Id { get; set; }
}

public class DeleteRetiredRecordCommandHandler : IRequestHandler<DeleteRetiredRecordCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly FileStorageService _files;
    private readonly AuditTrail _audit;
    private readonly IMediator _mediator;

    public DeleteRetiredRecordCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        FileStorageService files,
        AuditTrail audit,
        IMediator mediator)
    {
        _dbContext = dbContext;
        _access = access;
        _files = files;
        _audit = audit;
        _mediator = mediator;
    }

    /// <summary>
    ///     Atiende &lt;see cref="DeleteRetiredRecordCommand"/&gt;.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     La pertenencia vigente no admite borrado en cascada por su índice filtrado: se borra con el asistente.
    /// </para>
    /// <para>
    ///     Los objetos se borran solo después de confirmar el borrado de sus metadatos.
    /// </para>
    /// </remarks>
    public async Task<Unit> Handle(DeleteRetiredRecordCommand request, CancellationToken cancellationToken = default)
    {
        var impact = await _mediator.Send<GetRetiredImpactQuery, RetiredImpactResponse>(
            new GetRetiredImpactQuery { Type = request.Type, Id = request.Id }, cancellationToken);

        if (!impact.CanDelete)
        {
            throw new ConflictException(impact.BlockedReason!);
        }

        var objectKeys = new List<string>();

        switch (request.Type)
        {
            case RetiredType.Attendee:
                var attendee = await RetiredRecordHelpers.RetiredAttendeeAsync(_dbContext, request.Id, cancellationToken);
                _dbContext.Attendees.Remove(attendee);
                _audit.Record(AuditModule.Attendees, AuditActions.PermanentlyDeleted, attendee.Id, attendee.FullName, impact.Dependencies, null);
                break;
            case RetiredType.Group:
                var group = await RetiredRecordHelpers.RetiredGroupAsync(_dbContext, request.Id, cancellationToken);
                _dbContext.Groups.Remove(group);
                _audit.Record(AuditModule.Groups, AuditActions.PermanentlyDeleted, group.Id, group.Name, impact.Dependencies, null);
                break;
            case RetiredType.Event:
                var @event = await RetiredRecordHelpers.RetiredEventAsync(_dbContext, request.Id, cancellationToken);
                objectKeys.AddRange(await _dbContext.EventAttachments.IgnoreQueryFilters().Where(item => item.EventId == @event.Id).Select(item => item.ObjectKey).ToListAsync(cancellationToken));
                objectKeys.AddRange(await _dbContext.EventCommentAttachments.IgnoreQueryFilters().Where(item => item.Comment.EventId == @event.Id).Select(item => item.ObjectKey).ToListAsync(cancellationToken));
                _dbContext.Events.Remove(@event);
                _audit.Record(AuditModule.Events, AuditActions.PermanentlyDeleted, @event.Id, @event.Title, impact.Dependencies, null);
                break;
            case RetiredType.FollowUp:
                var followUp = await RetiredRecordHelpers.RetiredFollowUpAsync(_dbContext, _access, request.Id, cancellationToken);
                _dbContext.FollowUps.Remove(followUp);
                _audit.Record(AuditModule.FollowUps, AuditActions.PermanentlyDeleted, followUp.Id, "Seguimiento", null, null);
                break;
            default:
                var item = await RetiredRecordHelpers.FindCatalogAsync(_dbContext, request.Type, request.Id, cancellationToken);
                RemoveCatalog(item);
                _audit.Record(AuditModule.Configuration, AuditActions.PermanentlyDeleted, item.Id, $"{request.Type}: {item.Name}", new { item.Name }, null);
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _files.CompensateAsync(objectKeys);

        return Unit.Value;
    }

    private void RemoveCatalog(CatalogItem item)
    {
        switch (item)
        {
            case Generation generation: _dbContext.Generations.Remove(generation); break;
            case EventType eventType: _dbContext.EventTypes.Remove(eventType); break;
            case FollowUpTopic topic: _dbContext.FollowUpTopics.Remove(topic); break;
            case Course course: _dbContext.Courses.Remove(course); break;
        }
    }
}
