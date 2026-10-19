namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

public sealed class FollowUpContentResponse
{
    public required DateOnly MeetingDate { get; set; }
    public required string Location { get; set; }
    public required Guid TopicId { get; set; }
    public required string TopicName { get; set; }
    public required string Description { get; set; }
    public required string Commitments { get; set; }
    public string? Observations { get; set; }
}
