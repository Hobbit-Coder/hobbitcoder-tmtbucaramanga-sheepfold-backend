using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class PersonRef
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public UserRole? Role { get; set; }
}
