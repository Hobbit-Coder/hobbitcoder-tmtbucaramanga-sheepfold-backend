namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class GroupSummaryResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int Year { get; set; }
    public required GenerationRef Generation { get; set; }
    public required bool IsActive { get; set; }
    public required int AttendeeCount { get; set; }
    public required IReadOnlyList<TeamMember> Team { get; set; }
}
