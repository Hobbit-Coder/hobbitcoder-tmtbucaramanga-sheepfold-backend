using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class TeamMember
{
    public required Guid UserId { get; set; }
    public required string FullName { get; set; }
    public Gender? Gender { get; set; }
    public required GroupResponsibility Responsibility { get; set; }
}
