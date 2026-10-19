namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class CommentResponse
{
    public required Guid Id { get; set; }
    public Guid? ParentCommentId { get; set; }
    public required PersonRef Author { get; set; }
    public required string Body { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required IReadOnlyList<FileResponse> Attachments { get; set; }
}
