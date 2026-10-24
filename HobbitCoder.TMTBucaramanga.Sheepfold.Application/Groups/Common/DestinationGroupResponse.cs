namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Groups.Common;

/// <summary>Grupo al que se puede asignar o trasladar un asistente. Sin equipo ni integrantes.</summary>
public sealed class DestinationGroupResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int Year { get; set; }
    public required GenerationRef Generation { get; set; }
}
