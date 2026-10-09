using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Context;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Cryptography;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Authorization;

/// <summary>
/// Todos los endpoints exigen un token de Identity para Sheepfold; el administrador no accede a información
/// ministerial ni pastoral (§4.5, HU-04 caso 4) aunque su token sea válido.
/// </summary>
public class EndpointAuthorizationTests : IClassFixture<SheepfoldApiFixture>
{
    private const string AnyId = "01a0fd28-eb05-7fba-8260-b2345d93bcec";

    private readonly SheepfoldApiFixture _fixture;
    private readonly MinistryScenario _scenario;

    public EndpointAuthorizationTests(SheepfoldApiFixture fixture)
    {
        _fixture = fixture;
        _scenario = new MinistryScenario(fixture);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static TheoryData<string, string> ProtectedEndpoints() => new()
    {
        { "GET", "/api/me/context" },
        { "GET", "/api/calendar?from=2026-10-01&to=2026-10-31" },
        { "GET", "/api/events" },
        { "POST", "/api/events" },
        { "GET", $"/api/events/{AnyId}/attendance" },
        { "GET", "/api/attendees" },
        { "POST", "/api/attendees" },
        { "GET", "/api/groups" },
        { "GET", "/api/follow-ups" },
        { "GET", "/api/notifications" },
        { "GET", "/api/reports/Attendance?from=2026-10-01&to=2026-10-31" },
        { "GET", "/api/audit" },
        { "GET", "/api/retired" },
        { "GET", "/api/catalogs" },
        { "PUT", "/api/settings/indicators" }
    };

    /// <summary>Información ministerial y pastoral que el administrador nunca consulta.</summary>
    public static TheoryData<string> MinistryEndpoints() => new()
    {
        "/api/calendar?from=2026-10-01&to=2026-10-31",
        "/api/events",
        "/api/attendees",
        "/api/groups",
        "/api/follow-ups",
        "/api/reports/Attendance?from=2026-10-01&to=2026-10-31",
        "/api/audit",
        "/api/notifications"
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Endpoint_WithoutToken_Returns401(string method, string uri)
    {
        using var response = await _scenario.SendAsync(new HttpMethod(method), uri, token: null, body: new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(MinistryEndpoints))]
    public async Task Admin_IsForbiddenFromMinistryInformation(string uri)
    {
        var admin = _fixture.AddUser(UserRole.Admin);

        using var response = await _scenario.SendAsync(HttpMethod.Get, uri, admin.Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_IsPublic()
    {
        using var response = await _scenario.Client.GetAsync("/health", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_IsAccepted_AndSubjectIsTheAuditUser()
    {
        var groupId = await _scenario.CreateGroupAsync();

        using var scope = _fixture.Services.CreateScope();
        var group = await scope.ServiceProvider.GetRequiredService<SheepfoldContext>().Groups.SingleAsync(item => item.Id == groupId, CancellationToken);
        Assert.Equal(_scenario.Director.Id.ToString(), group.CreatedBy);
    }

    [Fact]
    public async Task Context_ReflectsTheRoleCapabilities()
    {
        var apprentice = _fixture.AddUser(UserRole.Apprentice);
        var admin = _fixture.AddUser(UserRole.Admin);

        var apprenticeContext = await _scenario.GetAsync<MyContextResponse>("/api/me/context", apprentice.Token);
        var adminContext = await _scenario.GetAsync<MyContextResponse>("/api/me/context", admin.Token);

        Assert.True(apprenticeContext.Capabilities.Ministry);
        Assert.False(apprenticeContext.Capabilities.FollowUps);
        Assert.False(apprenticeContext.Capabilities.CreateEvents);
        Assert.False(adminContext.Capabilities.Ministry);
        Assert.True(adminContext.Capabilities.ManageConfiguration);
        Assert.False(adminContext.Capabilities.Audit);
    }

    [Fact]
    public async Task TokenForAnotherAudience_Returns401()
    {
        var token = _fixture.CreateToken(audience: "HobbitCoder.TMTBucaramanga.Identity", role: UserRole.Director);

        using var response = await _scenario.SendAsync(HttpMethod.Get, "/api/groups", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(SheepfoldApiFixture.Issuer + "/")]
    [InlineData("https://otro-identity.tmtbucaramanga.test")]
    public async Task TokenFromAnotherIssuer_Returns401(string issuer)
    {
        var token = _fixture.CreateToken(issuer: issuer, role: UserRole.Director);

        using var response = await _scenario.SendAsync(HttpMethod.Get, "/api/groups", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithHs256_Returns401()
    {
        var symmetricKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "clave-de-prueba" };
        var token = _fixture.CreateToken(signingCredentials: new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256), role: UserRole.Director);

        using var response = await _scenario.SendAsync(HttpMethod.Get, "/api/groups", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
