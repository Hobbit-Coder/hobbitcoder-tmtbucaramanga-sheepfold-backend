namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;

public sealed class CatalogsResponse
{
    public required IReadOnlyList<CatalogItemResponse> Generations { get; set; }
    public required IReadOnlyList<CatalogItemResponse> EventTypes { get; set; }
    public required IReadOnlyList<CatalogItemResponse> FollowUpTopics { get; set; }
    public required IReadOnlyList<CatalogItemResponse> Courses { get; set; }
}
