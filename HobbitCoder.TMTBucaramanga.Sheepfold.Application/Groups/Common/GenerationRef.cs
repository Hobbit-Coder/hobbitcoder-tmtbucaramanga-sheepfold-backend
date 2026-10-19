using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

public sealed class GenerationRef
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required GenerationProfile Profile { get; set; }
}
