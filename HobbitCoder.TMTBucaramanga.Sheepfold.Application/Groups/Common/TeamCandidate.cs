using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class TeamCandidate
{
    public required Guid UserId { get; set; }
    public required string FullName { get; set; }
    public Gender? Gender { get; set; }
    public required UserRole Role { get; set; }
}
