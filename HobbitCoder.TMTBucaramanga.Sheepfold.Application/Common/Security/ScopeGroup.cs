using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;

public sealed class ScopeGroup
{
    public required Guid GroupId { get; set; }
    public required int Year { get; set; }
    public required Guid GenerationId { get; set; }
    public required bool IsActive { get; set; }
    public required GroupResponsibility Responsibility { get; set; }
}
