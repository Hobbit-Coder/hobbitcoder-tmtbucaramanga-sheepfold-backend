using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

/// <summary>
/// Datos generales de un seguimiento: persona, autor, estado y fechas. Nunca tema, lugar ni contenido: esos
/// datos solo van en el detalle autorizado.
/// </summary>
public sealed class FollowUpListItem
{
    public required Guid Id { get; set; }
    public required FollowUpSubject Subject { get; set; }
    public required Guid AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required FollowUpStatus Status { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public DateOnly? MeetingDate { get; set; }
    public required bool IsConfidential { get; set; }
    public required bool CanEdit { get; set; }
}
