using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

/// <summary>Datos generales de un seguimiento: nunca su contenido.</summary>
public sealed class FollowUpGeneralItem
{
    public required Guid Id { get; set; }
    public required FollowUpStatus Status { get; set; }
    public DateOnly? Date { get; set; }
    public required string AuthorName { get; set; }
    public required bool IsConfidential { get; set; }
}
