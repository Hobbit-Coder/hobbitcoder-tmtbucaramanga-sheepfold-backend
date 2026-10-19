namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed class AudienceGroupRef
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int Year { get; set; }
    public required Guid GenerationId { get; set; }
    public required string GenerationName { get; set; }
}
