using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.IntegrationTests.Authorization;

/// <summary>Todos los endpoints de TodoList y TodoItem exigen un token de Identity para Sheepfold.</summary>
public class TodoAuthorizationTests : IClassFixture<SheepfoldApiFixture>
{
    private const string AnyId = "01a0fd28-eb05-7fba-8260-b2345d93bcec";

    private readonly SheepfoldApiFixture _fixture;
    private readonly HttpClient _client;

    public TodoAuthorizationTests(SheepfoldApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static TheoryData<string, string> ProtectedEndpoints() => new()
    {
        { "GET", "/api/TodoList" },
        { "GET", $"/api/TodoList/{AnyId}" },
        { "POST", "/api/TodoList" },
        { "PUT", $"/api/TodoList/{AnyId}" },
        { "DELETE", $"/api/TodoList/{AnyId}" },
        { "GET", "/api/TodoItem" },
        { "GET", $"/api/TodoItem/{AnyId}" },
        { "POST", "/api/TodoItem" },
        { "PUT", $"/api/TodoItem/{AnyId}" },
        { "PATCH", $"/api/TodoItem/{AnyId}/status" },
        { "DELETE", $"/api/TodoItem/{AnyId}" }
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Endpoint_WithoutToken_Returns401(string method, string uri)
    {
        using var response = await SendAsync(new HttpMethod(method), uri, accessToken: null, body: new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_IsPublic()
    {
        using var response = await _client.GetAsync("/health", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_IsAccepted_AndSubjectIsTheAuditUser()
    {
        var subject = Guid.NewGuid().ToString();
        var token = _fixture.CreateToken(subject);

        using var created = await SendAsync(HttpMethod.Post, "/api/TodoList", token, new { title = "Lista con sesión", status = 1 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await created.Content.ReadFromJsonAsync<Guid>(CancellationToken);

        using var list = await SendAsync(HttpMethod.Get, "/api/TodoList", token);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SheepfoldContext>();
        var todoList = await dbContext.TodoLists.SingleAsync(todoList => todoList.Id == id, CancellationToken);
        Assert.Equal(subject, todoList.CreatedBy);
    }

    [Fact]
    public async Task TokenForAnotherAudience_Returns401()
    {
        var token = _fixture.CreateToken(audience: "HobbitCoder.TMTBucaramanga.Identity");

        using var response = await SendAsync(HttpMethod.Get, "/api/TodoList", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(SheepfoldApiFixture.Issuer + "/")]
    [InlineData("https://otro-identity.tmtbucaramanga.test")]
    public async Task TokenFromAnotherIssuer_Returns401(string issuer)
    {
        var token = _fixture.CreateToken(issuer: issuer);

        using var response = await SendAsync(HttpMethod.Get, "/api/TodoList", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithHs256_Returns401()
    {
        var symmetricKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "clave-de-prueba" };
        var token = _fixture.CreateToken(signingCredentials: new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256));

        using var response = await SendAsync(HttpMethod.Get, "/api/TodoList", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, string? accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, uri);

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request, CancellationToken);
    }
}
