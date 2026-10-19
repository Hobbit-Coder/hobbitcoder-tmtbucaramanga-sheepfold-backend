using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context.Common;

public sealed class MyContextResponse
{
    public required Guid UserId { get; set; }
    public required string FullName { get; set; }
    public UserRole? Role { get; set; }
    public required MyCapabilities Capabilities { get; set; }
    public required IReadOnlyList<MyGroupResponse> Groups { get; set; }
}
