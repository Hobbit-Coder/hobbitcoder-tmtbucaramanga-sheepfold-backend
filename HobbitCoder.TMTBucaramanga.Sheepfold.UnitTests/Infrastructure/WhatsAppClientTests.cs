using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.UnitTests.Infrastructure;

public class EvolutionWhatsAppClientTests
{
    private static readonly MessagingSettings Settings = new()
    {
        Evolution = new EvolutionSettings { Url = "https://evolution.example.com", ApiKey = "clave-de-prueba", Instance = "tmt", SecondsBetweenMessages = 3 }
    };

    [Fact]
    public async Task ChecksTheNumber_ThenSendsTitleAndMessageAsText()
    {
        var handler = new RoutingHandler()
            .On("/chat/whatsappNumbers/tmt", HttpStatusCode.OK, """[{"jid":"573001234567@s.whatsapp.net","exists":true,"number":"573001234567"}]""")
            .On("/message/sendText/tmt", HttpStatusCode.Created, """{"key":{"id":"ABC"},"status":"PENDING"}""");

        var sent = await NewClient(handler).SendAsync(
            new WhatsAppNotification { PhoneNumber = "+57 300 123 4567", Title = "Próximo evento: Retiro", Message = "Retiro empieza el 12/10/2026 a las 08:00." },
            TestContext.Current.CancellationToken);

        Assert.True(sent);
        var (check, checkBody) = handler.Requests[0];
        Assert.Equal("https://evolution.example.com/chat/whatsappNumbers/tmt", check.RequestUri!.ToString());
        Assert.Equal("573001234567", JsonDocument.Parse(checkBody!).RootElement.GetProperty("numbers")[0].GetString());
        var (send, body) = handler.Requests[1];
        Assert.Equal("clave-de-prueba", send.Headers.GetValues("apikey").Single());
        var json = JsonDocument.Parse(body!).RootElement;
        Assert.Equal("573001234567", json.GetProperty("number").GetString());
        Assert.Equal("*Próximo evento: Retiro*\nRetiro empieza el 12/10/2026 a las 08:00.\n\n_TMT Bucaramanga_", json.GetProperty("text").GetString());
    }

    [Fact]
    public async Task NumberWithoutWhatsApp_IsSkipped()
    {
        var handler = new RoutingHandler().On("/chat/whatsappNumbers/tmt", HttpStatusCode.OK, """[{"jid":"573001234567@s.whatsapp.net","exists":false,"number":"573001234567"}]""");

        Assert.False(await NewClient(handler).SendAsync(new WhatsAppNotification { PhoneNumber = "+573001234567", Title = "Aviso", Message = "Mensaje" }, TestContext.Current.CancellationToken));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task EvolutionRejection_IsLoggedAndNotThrown()
    {
        var handler = new RoutingHandler()
            .On("/chat/whatsappNumbers/tmt", HttpStatusCode.OK, """[{"jid":"573001234567@s.whatsapp.net","exists":true,"number":"573001234567"}]""")
            .On("/message/sendText/tmt", HttpStatusCode.BadRequest, """{"status":400,"error":"Bad Request","response":{"message":["Connection Closed"]}}""");

        Assert.False(await NewClient(handler).SendAsync(new WhatsAppNotification { PhoneNumber = "+573001234567", Title = "Aviso", Message = "Mensaje" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void PausesBetweenMessages_AndNeedsUrlKeyAndInstance()
    {
        Assert.Equal(TimeSpan.FromSeconds(3), NewClient(new RoutingHandler()).PauseBetweenMessages);
        Assert.False(new EvolutionWhatsAppClient(new SingleClientFactory(new RoutingHandler()),
            Options.Create(new MessagingSettings { Evolution = new EvolutionSettings { Url = "https://evolution.example.com", ApiKey = "clave" } }),
            NullLogger<EvolutionWhatsAppClient>.Instance).IsConfigured);
    }

    private static EvolutionWhatsAppClient NewClient(RoutingHandler handler) =>
        new(new SingleClientFactory(handler), Options.Create(Settings), NullLogger<EvolutionWhatsAppClient>.Instance);
}

public class MetaWhatsAppClientTests
{
    private static readonly MessagingSettings Settings = new()
    {
        Provider = MessagingProvider.Meta,
        Meta = new MetaWhatsAppSettings { PhoneNumberId = "1234567890", AccessToken = "token-de-prueba" }
    };

    [Fact]
    public async Task SendsTheUtilityTemplate_WithTitleAndMessage()
    {
        var handler = new RoutingHandler().On("/v24.0/1234567890/messages", HttpStatusCode.OK, """{"messages":[{"id":"wamid.1"}]}""");

        var sent = await NewClient(handler).SendAsync(
            new WhatsAppNotification { PhoneNumber = "+57 300 123 4567", Title = "Próximo evento: Retiro", Message = "Retiro empieza el 12/10/2026\na las 08:00 en   Sede." },
            TestContext.Current.CancellationToken);

        Assert.True(sent);
        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal("token-de-prueba", request.Headers.Authorization!.Parameter);
        var template = JsonDocument.Parse(body!).RootElement.GetProperty("template");
        Assert.Equal("tmt_aviso", template.GetProperty("name").GetString());
        var parameters = template.GetProperty("components")[0].GetProperty("parameters");
        Assert.Equal("Próximo evento: Retiro", parameters[0].GetProperty("text").GetString());
        Assert.Equal("Retiro empieza el 12/10/2026 a las 08:00 en Sede.", parameters[1].GetProperty("text").GetString());
    }

    [Fact]
    public async Task ProviderRejection_IsLoggedAndNotThrown()
    {
        var handler = new RoutingHandler().On("/v24.0/1234567890/messages", HttpStatusCode.BadRequest, """{"error":{"code":131026,"message":"Message undeliverable"}}""");

        Assert.False(await NewClient(handler).SendAsync(new WhatsAppNotification { PhoneNumber = "+573001234567", Title = "Aviso", Message = "Mensaje" }, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Hola", 10, "Hola")]
    [InlineData("  línea 1\n\tlínea 2     fin  ", 100, "línea 1 línea 2 fin")]
    [InlineData("abcdefghijk", 6, "abcde…")]
    public void Parameter_IsCompactedAndBounded(string value, int maxLength, string expected) =>
        Assert.Equal(expected, MetaWhatsAppClient.Parameter(value, maxLength));

    [Theory]
    [InlineData("""{"error":{"code":190,"message":"Invalid OAuth access token"}}""", "190 Invalid OAuth access token")]
    [InlineData("""{"status":404,"error":"Not Found","response":{"message":["Instance not found"]}}""", "Instance not found")]
    [InlineData("<html>bad gateway</html>", "sin detalle")]
    [InlineData("""{"error":"texto"}""", "sin detalle")]
    public async Task ErrorSummary_KeepsOnlyCodeAndMessage(string body, string expected)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };

        Assert.Equal(expected, await WhatsAppError.ReadAsync(response, TestContext.Current.CancellationToken));
    }

    private static MetaWhatsAppClient NewClient(RoutingHandler handler) =>
        new(new SingleClientFactory(handler), Options.Create(Settings), NullLogger<MetaWhatsAppClient>.Instance);
}

internal sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

/// <summary>Responde según la ruta pedida y guarda cada solicitud con su cuerpo.</summary>
internal sealed class RoutingHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _routes = [];

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public RoutingHandler On(string path, HttpStatusCode status, string body)
    {
        _routes[path] = (status, body);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

        return _routes.TryGetValue(request.RequestUri!.AbsolutePath, out var route)
            ? new HttpResponseMessage(route.Status) { Content = new StringContent(route.Body) }
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
