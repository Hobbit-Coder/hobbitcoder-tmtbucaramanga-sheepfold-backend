using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Evento del calendario (§15). El creador es <see cref="AuditableEntity.CreatedBy"/>. "General o por grupos"
/// se deriva del público: <see cref="EventAudience.Groups"/> es por grupos.
/// </summary>
public class Event : SoftDeleteableEntity
{
    public required string Title { get; set; }
    public required Guid EventTypeId { get; set; }
    public required DateTime StartsAt { get; set; }
    public required DateTime EndsAt { get; set; }
    public required string Location { get; set; }
    public required string Description { get; set; }
    public required EventAudience Audience { get; set; }
    public bool RequiresAttendance { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;

    public virtual EventType EventType { get; set; } = null!;
    public virtual ICollection<EventGeneration> Generations { get; set; } = [];
    public virtual ICollection<EventGroup> Groups { get; set; } = [];
    public virtual ICollection<EventManager> Managers { get; set; } = [];
    public virtual ICollection<EventAttachment> Attachments { get; set; } = [];
    public virtual ICollection<EventComment> Comments { get; set; } = [];

    public bool IsByGroups => Audience == EventAudience.Groups;
}

/// <summary>Generación del público de un evento (público <see cref="EventAudience.Generations"/>).</summary>
public class EventGeneration
{
    public required Guid EventId { get; set; }
    public required Guid GenerationId { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual Generation Generation { get; set; } = null!;
}

/// <summary>Grupo del público de un evento (público <see cref="EventAudience.Groups"/>).</summary>
public class EventGroup
{
    public required Guid EventId { get; set; }
    public required Guid GroupId { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}

/// <summary>Encargado del evento (§15, §16): lo administra sin ganar acceso a otros eventos o grupos.</summary>
public class EventManager
{
    public required Guid EventId { get; set; }
    public required Guid UserId { get; set; }

    public virtual Event Event { get; set; } = null!;
}

/// <summary>
/// Archivo adjunto al evento. El contenido vive en el almacenamiento S3 privado; aquí solo los metadatos.
/// </summary>
public class EventAttachment : SoftDeleteableEntity, IStoredFile
{
    public required Guid EventId { get; set; }
    public required string ObjectKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }

    public virtual Event Event { get; set; } = null!;
}

/// <summary>Comentario de la discusión del evento (§18). Una respuesta apunta a un comentario raíz.</summary>
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

/// <summary>Metadatos de un archivo guardado en el almacenamiento de objetos.</summary>
public interface IStoredFile
{
    string ObjectKey { get; }
    string FileName { get; }
    string ContentType { get; }
    long SizeBytes { get; }
}

/// <summary>
/// Marca de asistencia de un asistente en un evento (§19). Clave (evento, asistente): una marca vigente por
/// persona. Sin fila = sin marca, que se interpreta como no asistió. Quién la registró queda en
/// <c>CreatedBy</c>/<c>UpdatedBy</c> y cada cambio en la auditoría.
/// </summary>
public class AttendeeAttendance : IAuditableEntity
{
    public required Guid EventId { get; set; }
    public required Guid AttendeeId { get; set; }
    public required AttendanceMark Mark { get; set; }

    public string CreatedBy { get; set; } = Guid.Empty.ToString();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual Event Event { get; set; } = null!;
    public virtual Attendee Attendee { get; set; } = null!;
}

/// <summary>Marca de asistencia de un usuario (cuenta de Identity) en un evento (§19). Clave (evento, usuario).</summary>
public class UserAttendance : IAuditableEntity
{
    public required Guid EventId { get; set; }
    public required Guid UserId { get; set; }
    public required AttendanceMark Mark { get; set; }

    public string CreatedBy { get; set; } = Guid.Empty.ToString();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual Event Event { get; set; } = null!;
}
