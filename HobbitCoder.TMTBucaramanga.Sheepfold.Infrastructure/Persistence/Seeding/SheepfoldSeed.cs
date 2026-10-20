using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Seeding;

/// <summary>
/// Carga inicial del ministerio 2026 (grupos, responsabilidades, asistentes con sus cursos y contactos, y el
/// cronograma de eventos). La genera <c>docs/resources/bases-grupos/generar-semillas.py</c> a partir de los
/// Excel y viaja incrustada en el ensamblado. Los <c>UserId</c> de las responsabilidades son las cuentas que
/// siembra Identity con los mismos Id.
/// </summary>
public class SheepfoldSeed
{
    public const string ResourceName = "SheepfoldSeed.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public List<Generation> Generations { get; set; } = [];
    public List<Course> Courses { get; set; } = [];
    public List<EventType> EventTypes { get; set; } = [];
    public List<Group> Groups { get; set; } = [];
    public List<GroupAssignment> Assignments { get; set; } = [];
    public List<Attendee> Attendees { get; set; } = [];
    public List<Event> Events { get; set; } = [];

    public static SheepfoldSeed Load()
    {
        using var stream = typeof(SheepfoldSeed).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"No se encontró el recurso incrustado {ResourceName}.");

        return JsonSerializer.Deserialize<SheepfoldSeed>(stream, Options)
            ?? throw new InvalidOperationException($"{ResourceName} está vacío.");
    }
}
