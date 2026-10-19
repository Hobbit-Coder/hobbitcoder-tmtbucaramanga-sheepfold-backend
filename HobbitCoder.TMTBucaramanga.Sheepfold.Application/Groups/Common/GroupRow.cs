using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class GroupRow
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int Year { get; set; }
    public required Guid GenerationId { get; set; }
    public required string GenerationName { get; set; }
    public required GenerationProfile Profile { get; set; }
    public required bool IsActive { get; set; }
    public required int AttendeeCount { get; set; }
    public required List<AssignmentRow> Assignments { get; set; }

    public GroupSummaryResponse ToResponse(IReadOnlyDictionary<Guid, DirectoryUser> directory) => new GroupSummaryResponse
    {
        Id = Id,
        Name = Name,
        Year = Year,
        Generation = new GenerationRef { Id = GenerationId, Name = GenerationName, Profile = Profile },
        IsActive = IsActive,
        AttendeeCount = AttendeeCount,
        Team = Assignments
            .OrderBy(assignment => assignment.Responsibility)
            .ThenBy(assignment => directory.NameOf(assignment.UserId))
            .Select(assignment => new TeamMember
            {
                UserId = assignment.UserId,
                FullName = directory.NameOf(assignment.UserId),
                Gender = directory.GetValueOrDefault(assignment.UserId)?.Gender,
                Responsibility = assignment.Responsibility
            })
            .ToList()
    };
}
