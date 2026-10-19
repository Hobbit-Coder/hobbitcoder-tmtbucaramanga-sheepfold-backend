using HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Authorization;

/// <summary>Contrato HTTP de los controladores: cabecera Location de CreatedAtAction, rutas y códigos de estado.</summary>
public class ControllerContractTests : IClassFixture<SheepfoldApiFixture>
{
    private readonly MinistryScenario _scenario;

    public ControllerContractTests(SheepfoldApiFixture fixture)
    {
        _scenario = new MinistryScenario(fixture);
    }

    [Fact]
    public async Task CreateGroup_ReturnsCreatedWithLocationThatResolves()
    {
        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/groups", _scenario.Director.Token,
            new { year = 2026, generationId = MinistryScenario.Eco, name = $"Grupo {Guid.NewGuid().ToString()[..8]}" });
        var id = await MinistryScenario.ReadAsync<Guid>(created);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal($"/api/Groups/{id}", created.Headers.Location!.AbsolutePath, ignoreCase: true);

        using var fetched = await _scenario.SendAsync(HttpMethod.Get, created.Headers.Location.AbsolutePath, _scenario.Director.Token);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    [Fact]
    public async Task CreateAttendee_ReturnsCreatedWithLocationThatResolves()
    {
        var groupId = await _scenario.CreateGroupAsync();
        using var created = await _scenario.SendAsync(HttpMethod.Post, "/api/attendees", _scenario.Director.Token, new
        {
            firstName = "Ana",
            lastName = "Prueba",
            gender = "Female",
            birthDate = "2008-10-09",
            isServer = false,
            groupId,
            contacts = new[] { new { kind = "EmergencyContact", fullName = "Carmen", relationship = "Madre", phoneNumber = "+573000000102" } }
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var fetched = await _scenario.SendAsync(HttpMethod.Get, created.Headers.Location!.AbsolutePath, _scenario.Director.Token);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    [Theory]
    [InlineData("/api/groups")]
    [InlineData("/api/Groups")]
    [InlineData("/api/events")]
    [InlineData("/api/catalogs")]
    public async Task Get_RoutesFromControllerTemplate_ReturnOk(string uri)
    {
        using var response = await _scenario.SendAsync(HttpMethod.Get, uri, _scenario.Director.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGroup_Unknown_Returns404()
    {
        using var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/groups/{Guid.NewGuid()}", _scenario.Director.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RetireGroup_Returns204()
    {
        var groupId = await _scenario.CreateGroupAsync();

        using var response = await _scenario.SendAsync(HttpMethod.Delete, $"/api/groups/{groupId}", _scenario.Director.Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task FollowUpsAndUsersKeepTheirExplicitRoutes()
    {
        using var followUps = await _scenario.SendAsync(HttpMethod.Get, "/api/follow-ups", _scenario.Director.Token);
        using var oldWrongRoute = await _scenario.SendAsync(HttpMethod.Get, "/api/followups", _scenario.Director.Token);

        Assert.Equal(HttpStatusCode.OK, followUps.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, oldWrongRoute.StatusCode);
    }
}
