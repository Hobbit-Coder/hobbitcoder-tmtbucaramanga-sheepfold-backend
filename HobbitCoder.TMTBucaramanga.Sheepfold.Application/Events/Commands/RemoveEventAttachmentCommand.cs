using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;

/// <summary>Retira un adjunto del evento; el objeto se conserva hasta la eliminación definitiva.</summary>
public class RemoveEventAttachmentCommand : IRequest
{
    public Guid EventId { get; set; }
    public Guid AttachmentId { get; set; }
}

public class RemoveEventAttachmentCommandHandler : IRequestHandler<RemoveEventAttachmentCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public RemoveEventAttachmentCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(RemoveEventAttachmentCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);

        if (!_access.CanAdministerEvent(@event))
        {
            throw new ForbiddenAccessException();
        }

        var attachment = await _dbContext.EventAttachments.FirstOrDefaultAsync(item => item.Id == request.AttachmentId && item.EventId == @event.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EventAttachment), request.AttachmentId);

        _dbContext.EventAttachments.Remove(attachment);
        _audit.Record(AuditModule.Events, AuditActions.AttachmentRemoved, @event.Id, $"{@event.Title} / {attachment.FileName}",
            new { attachment.FileName, attachment.ContentType, attachment.SizeBytes }, null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
