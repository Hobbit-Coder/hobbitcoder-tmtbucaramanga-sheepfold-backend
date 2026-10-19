using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Commands;

/// <summary>Adjunta un archivo al evento. Quien administra el evento.</summary>
public class AddEventAttachmentCommand : IRequest<FileResponse>
{
    public Guid EventId { get; set; }
    public required UploadedFile File { get; set; }
}

public class AddEventAttachmentCommandHandler : IRequestHandler<AddEventAttachmentCommand, FileResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly FileStorageService _files;
    private readonly IUserDirectory _userDirectory;
    private readonly AuditTrail _audit;

    public AddEventAttachmentCommandHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        FileStorageService files,
        IUserDirectory userDirectory,
        AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _files = files;
        _userDirectory = userDirectory;
        _audit = audit;
    }

    public async Task<FileResponse> Handle(AddEventAttachmentCommand request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);

        if (!_access.CanAdministerEvent(@event))
        {
            throw new ForbiddenAccessException();
        }

        var upload = (await _files.UploadAsync($"events/{@event.Id:N}", [request.File], cancellationToken))[0];
        var attachment = new EventAttachment
        {
            EventId = @event.Id,
            ObjectKey = upload.ObjectKey,
            FileName = upload.FileName,
            ContentType = upload.ContentType,
            SizeBytes = upload.SizeBytes
        };

        try
        {
            _dbContext.EventAttachments.Add(attachment);
            _audit.Record(AuditModule.Events, AuditActions.AttachmentAdded, @event.Id, $"{@event.Title} / {attachment.FileName}", null,
                new { attachment.FileName, attachment.ContentType, attachment.SizeBytes });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _files.CompensateAsync([upload.ObjectKey]);
            throw;
        }

        var directory = await _userDirectory.GetUsersAsync(cancellationToken);
        return new FileResponse { Id = attachment.Id, FileName = attachment.FileName, ContentType = attachment.ContentType, SizeBytes = attachment.SizeBytes, UploadedAt = attachment.CreatedDate, UploadedBy = directory.NameOf(attachment.CreatedBy) };
    }
}
