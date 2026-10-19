using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

/// <summary>Públicos y opciones que el usuario puede elegir al crear o editar un evento.</summary>
public sealed class AudienceOptionsResponse
{
    public required IReadOnlyList<EventAudience> Audiences { get; set; }
    public required IReadOnlyList<NamedRef> Generations { get; set; }
    public required IReadOnlyList<AudienceGroupRef> Groups { get; set; }
    public required IReadOnlyList<PersonRef> Managers { get; set; }
}
