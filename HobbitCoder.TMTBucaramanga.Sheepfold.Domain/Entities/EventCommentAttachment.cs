using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Archivo adjunto a un comentario. Contenido en S3; aquí solo los metadatos.</summary>
public class EventCommentAttachment : AuditableEntity, IStoredFile
{
    public required Guid CommentId { get; set; }
    public required string ObjectKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }

    public virtual EventComment Comment { get; set; } = null!;
}
