using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Queries;

public class GetCommentAttachmentContentQuery : IRequest<FileContent>
{
    public Guid EventId { get; set; }
    public Guid CommentId { get; set; }
    public Guid AttachmentId { get; set; }
}

public class GetCommentAttachmentContentQueryHandler : IRequestHandler<GetCommentAttachmentContentQuery, FileContent>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly FileStorageService _files;

    public GetCommentAttachmentContentQueryHandler(
        IApplicationDbContext dbContext,
        MinistryAccess access,
        FileStorageService files)
    {
        _dbContext = dbContext;
        _access = access;
        _files = files;
    }

    public async Task<FileContent> Handle(GetCommentAttachmentContentQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        var attachment = await _dbContext.EventCommentAttachments.FirstOrDefaultAsync(item => item.Id == request.AttachmentId
                && item.CommentId == request.CommentId && item.Comment.EventId == @event.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EventCommentAttachment), request.AttachmentId);

        return new FileContent { Content = await _files.OpenAsync(attachment.ObjectKey, cancellationToken), FileName = attachment.FileName, ContentType = attachment.ContentType };
    }
}
