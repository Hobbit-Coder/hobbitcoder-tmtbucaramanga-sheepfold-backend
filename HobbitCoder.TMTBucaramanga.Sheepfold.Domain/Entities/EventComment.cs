using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Comentario de la discusión del evento. Una respuesta apunta a un comentario raíz.</summary>
public class EventComment : AuditableEntity
{
    public required Guid EventId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public required string Body { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual EventComment? ParentComment { get; set; }
    public virtual ICollection<EventComment> Replies { get; set; } = [];
    public virtual ICollection<EventCommentAttachment> Attachments { get; set; } = [];
}
