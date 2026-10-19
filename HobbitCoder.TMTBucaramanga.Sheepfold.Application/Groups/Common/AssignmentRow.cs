using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class AssignmentRow
{
    public required Guid UserId { get; set; }
    public required GroupResponsibility Responsibility { get; set; }
}
