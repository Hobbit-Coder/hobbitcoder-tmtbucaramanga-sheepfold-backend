namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

/// <summary>Información de la reunión realizada. Las observaciones son opcionales.</summary>
public class FollowUpContent
{
    public DateOnly MeetingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public Guid TopicId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Commitments { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public bool IsConfidential { get; set; }
}
