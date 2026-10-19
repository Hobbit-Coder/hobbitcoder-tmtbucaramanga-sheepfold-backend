using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;

/// <summary>
/// Organización ministerial mínima creada por la API, como lo haría un director: grupos con su equipo, asistentes y
/// eventos. Cada escenario usa nombres únicos para no chocar con otras pruebas que comparten la base.
/// </summary>
public sealed class MinistryScenario
{
    public static readonly Guid Pulso = Guid.Parse("0199c2a0-0000-7000-8000-000000000101");
    public static readonly Guid Eco = Guid.Parse("0199c2a0-0000-7000-8000-000000000102");
    public static readonly Guid EventTypeId = Guid.Parse("0199c2a0-0000-7000-8000-000000000202");
    public static readonly Guid CourseId = Guid.Parse("0199c2a0-0000-7000-8000-000000000301");

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public MinistryScenario(SheepfoldApiFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateApiClient();
        Director = fixture.AddUser(UserRole.Director, Gender.Female);
    }

    public SheepfoldApiFixture Fixture { get; }
    public HttpClient Client { get; }
    public TestUser Director { get; }

    public async Task<Guid> CreateGroupAsync(Guid? generationId = null, int year = 2026)
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/groups", Director.Token,
            new { year, generationId = generationId ?? Eco, name = $"Grupo {Guid.NewGuid().ToString()[..8]}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadAsync<Guid>(response);
    }

    public async Task SetTeamAsync(Guid groupId, Guid[]? coordinators = null, Guid[]? leaders = null, Guid[]? apprentices = null)
    {
        using var response = await SendAsync(HttpMethod.Put, $"/api/groups/{groupId}/team", Director.Token,
            new { coordinatorIds = coordinators ?? [], leaderIds = leaders ?? [], apprenticeIds = apprentices ?? [] });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>Asistente con el contacto que exige su generación (tutor para PULSO, emergencia para las demás).</summary>
    public async Task<Guid> CreateAttendeeAsync(Guid groupId, bool isServer = false, ContactKind contactKind = ContactKind.EmergencyContact, string? token = null)
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/attendees", token ?? Director.Token, new
        {
            firstName = "Ana",
            lastName = $"Pérez {Guid.NewGuid().ToString()[..6]}",
            gender = Gender.Female,
            birthDate = new DateOnly(2008, 10, 9),
            isServer,
            groupId,
            contacts = new[] { new { kind = contactKind, fullName = "Carmen Pérez", relationship = "Madre", phoneNumber = "+573000000102" } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadAsync<Guid>(response);
    }

    /// <summary>
    /// Inicio por defecto de los eventos de prueba: un jueves de octubre del año siguiente, siempre futuro. Así la
    /// población del evento (grupo a la fecha de inicio) incluye a los asistentes recién registrados sin depender
    /// de la hora en que corren las pruebas.
    /// </summary>
    public static readonly DateTimeOffset ReferenceEventStart = new(DateTime.UtcNow.Year + 1, 10, 8, 18, 0, 0, TimeSpan.FromHours(-5));

    /// <summary>Parámetros <c>from</c>/<c>to</c> del mes de <see cref="ReferenceEventStart"/>.</summary>
    public static string ReferenceMonth => $"from={ReferenceEventStart.Year}-10-01&to={ReferenceEventStart.Year}-10-31";

    public async Task<Guid> CreateEventAsync(
        string token, EventAudience audience, Guid[]? groupIds = null, Guid[]? managers = null, bool requiresAttendance = true,
        bool publish = true, DateTimeOffset? startsAt = null, Guid[]? generationIds = null)
    {
        var start = startsAt ?? ReferenceEventStart;
        using var response = await SendAsync(HttpMethod.Post, "/api/events", token, new
        {
            title = $"Estudio {Guid.NewGuid().ToString()[..6]}",
            eventTypeId = EventTypeId,
            startsAt = start,
            endsAt = start.AddMinutes(90),
            location = "Salón principal",
            description = "Estudio bíblico semanal.",
            audience,
            generationIds = generationIds ?? [],
            groupIds = groupIds ?? [],
            requiresAttendance,
            managerIds = managers ?? [Director.Id],
            publish
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadAsync<Guid>(response);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, string? token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, uri);

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return await Client.SendAsync(request, CancellationToken);
    }

    public async Task<T> GetAsync<T>(string uri, string token)
    {
        using var response = await SendAsync(HttpMethod.Get, uri, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await ReadAsync<T>(response);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<T>(JsonOptions, CancellationToken);
        Assert.NotNull(content);

        return content;
    }
}

/// <summary>Página de resultados tal como la serializa la API.</summary>
public sealed class Page<T>
{
    public required List<T> Items { get; set; }
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
    public required int TotalCount { get; set; }
    public required int TotalPages { get; set; }
}
