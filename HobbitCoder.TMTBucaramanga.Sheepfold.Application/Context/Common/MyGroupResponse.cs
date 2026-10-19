using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;

public sealed class MyGroupResponse
{
    public required Guid GroupId { get; set; }
    public required string Name { get; set; }
    public required int Year { get; set; }
    public required string GenerationName { get; set; }
    public required GroupResponsibility Responsibility { get; set; }
    public required bool IsActive { get; set; }
}
