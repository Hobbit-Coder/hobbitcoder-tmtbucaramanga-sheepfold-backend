using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Queries;

public class GetEventCommentsQuery : IRequest<List<CommentResponse>>
{
    public Guid EventId { get; set; }
}

public class GetEventCommentsQueryHandler : IRequestHandler<GetEventCommentsQuery, List<CommentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly IUserDirectory _userDirectory;

    public GetEventCommentsQueryHandler(IApplicationDbContext dbContext, MinistryAccess access, IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _access = access;
        _userDirectory = userDirectory;
    }

    public async Task<List<CommentResponse>> Handle(GetEventCommentsQuery request, CancellationToken cancellationToken = default)
    {
        var @event = await _access.FindVisibleEventAsync(request.EventId, cancellationToken);
        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        var comments = await _dbContext.EventComments
            .Where(comment => comment.EventId == @event.Id)
            .OrderBy(comment => comment.CreatedDate)
            .Select(comment => new
            {
                comment.Id,
                comment.ParentCommentId,
                comment.CreatedBy,
                comment.Body,
                comment.CreatedDate,
                Attachments = comment.Attachments.Select(attachment => new { attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedDate }).ToList()
            })
            .ToListAsync(cancellationToken);

        return comments.Select(comment => new CommentResponse
        {
            Id = comment.Id,
            ParentCommentId = comment.ParentCommentId,
            Author = EventCollaborationHelpers.Author(directory, comment.CreatedBy),
            Body = comment.Body,
            CreatedAt = comment.CreatedDate,
            Attachments = comment.Attachments.Select(item => new FileResponse { Id = item.Id, FileName = item.FileName, ContentType = item.ContentType, SizeBytes = item.SizeBytes, UploadedAt = item.CreatedDate, UploadedBy = directory.NameOf(comment.CreatedBy) }).ToList()
        })
            .ToList();
    }
}
