using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Queries;

/// <summary>Descarga autorizada: solo quien puede consultar el evento.</summary>
public class GetEventAttachmentContentQuery : IRequest<FileContent>
{
    public Guid EventId { get; set; }
    public Guid AttachmentId { get; set; }
}

public class GetEventAttachmentContentQueryHandler : IRequestHandler<GetEventAttachmentContentQuery, FileContent>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly FileStorageService _files;

    public GetEventAttachmentContentQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, FileStorageService files)
    {
        _dbContext = dbContext;
        _access = access;
        _files = files;
    }

    public async Task<FileContent> Handle(GetEventAttachmentContentQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        var attachment = await _dbContext.EventAttachments.FirstOrDefaultAsync(item => item.Id == request.AttachmentId && item.EventId == @event.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EventAttachment), request.AttachmentId);

        return new FileContent { Content = await _files.OpenAsync(attachment.ObjectKey, cancellationToken), FileName = attachment.FileName, ContentType = attachment.ContentType };
    }
}
