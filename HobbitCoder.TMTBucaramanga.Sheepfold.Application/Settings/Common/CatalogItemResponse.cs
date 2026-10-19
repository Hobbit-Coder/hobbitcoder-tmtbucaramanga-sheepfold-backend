using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;

public sealed class CatalogItemResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required bool IsActive { get; set; }
    public GenerationProfile? Profile { get; set; }
    public int? SortOrder { get; set; }
}
