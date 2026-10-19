namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Retired.Common;

/// <summary>Dependencias que se borrarían con el registro: solo conteos, nunca contenido.</summary>
public sealed class RetiredImpactResponse
{
    public required RetiredType Type { get; set; }
    public required Guid Id { get; set; }
    public required string Label { get; set; }
    public required IReadOnlyDictionary<string, int> Dependencies { get; set; }
    public required bool CanDelete { get; set; }
    public string? BlockedReason { get; set; }
}
