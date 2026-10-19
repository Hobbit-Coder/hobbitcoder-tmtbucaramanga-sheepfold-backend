using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Seguimiento pastoral. Se realiza a un asistente o a un usuario inferior en la jerarquía: exactamente
/// uno de <see cref="SubjectAttendeeId"/> y <see cref="SubjectUserId"/>. Su autor es
/// <see cref="AuditableEntity.CreatedBy"/>. Pendiente puede tener fecha (y hora) programada; completarlo
/// actualiza este mismo registro con la información de la reunión.
/// </summary>
public class FollowUp : SoftDeleteableEntity
{
    public Guid? SubjectAttendeeId { get; set; }

    /// <summary>Cuenta de Identity; sin clave foránea entre bases.</summary>
    public Guid? SubjectUserId { get; set; }

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;

    /// <summary>Fecha programada opcional de un pendiente.</summary>
    public DateOnly? ScheduledDate { get; set; }

    /// <summary>Hora programada, solo si hay fecha.</summary>
    public TimeOnly? ScheduledTime { get; set; }

    public DateOnly? MeetingDate { get; set; }
    public string? Location { get; set; }
    public Guid? TopicId { get; set; }
    public string? Description { get; set; }
    public string? Commitments { get; set; }
    public string? Observations { get; set; }
    public bool IsConfidential { get; set; }

    /// <summary>
    /// Grupo del asistente en la fecha de la reunión, fijado al realizarse: el registro conserva el grupo de
    /// ese momento aunque la persona cambie de grupo después. Nulo en pendientes y en
    /// seguimientos a usuarios; entonces se usa el grupo actual.
    /// </summary>
    public Guid? GroupId { get; set; }

    public virtual Attendee? SubjectAttendee { get; set; }
    public virtual Group? Group { get; set; }
    public virtual FollowUpTopic? Topic { get; set; }

    public Guid AuthorUserId => Guid.Parse(CreatedBy);
}
