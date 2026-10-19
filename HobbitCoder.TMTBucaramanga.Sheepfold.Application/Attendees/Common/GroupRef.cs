using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public sealed class GroupRef
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int Year { get; set; }
    public required Guid GenerationId { get; set; }
    public required string GenerationName { get; set; }
    public required GenerationProfile GenerationProfile { get; set; }
}
