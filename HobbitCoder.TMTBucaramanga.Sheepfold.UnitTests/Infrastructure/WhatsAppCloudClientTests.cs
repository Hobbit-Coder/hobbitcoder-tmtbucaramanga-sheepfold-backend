using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.UnitTests.Infrastructure;

public class WhatsAppCloudClientTests
{
    private static readonly MessagingSettings Settings = new()
    {
        PhoneNumberId = "1234567890",
        AccessToken = "token-de-prueba",
        NotificationTemplate = "tmt_aviso",
        TemplateLanguage = "es"
    };

    [Fact]
    public async Task SendsTheUtilityTemplate_WithTitleAndMessage()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"messages":[{"id":"wamid.1"}]}""");

        var sent = await NewClient(handler).SendAsync(
            new WhatsAppNotification("+57 300 123 4567", "Próximo evento: Retiro", "Retiro empieza el 12/10/2026\na las 08:00 en   Sede."),
            TestContext.Current.CancellationToken);

        Assert.True(sent);
        Assert.Equal("https://graph.facebook.com/v24.0/1234567890/messages", handler.Request!.RequestUri!.ToString());
        Assert.Equal("token-de-prueba", handler.Request.Headers.Authorization!.Parameter);

        var body = JsonDocument.Parse(handler.Body!).RootElement;
        Assert.Equal("573001234567", body.GetProperty("to").GetString());
        var template = body.GetProperty("template");
        Assert.Equal("tmt_aviso", template.GetProperty("name").GetString());
        Assert.Equal("es", template.GetProperty("language").GetProperty("code").GetString());
        var parameters = template.GetProperty("components")[0].GetProperty("parameters");
        Assert.Equal("Próximo evento: Retiro", parameters[0].GetProperty("text").GetString());
        Assert.Equal("Retiro empieza el 12/10/2026 a las 08:00 en Sede.", parameters[1].GetProperty("text").GetString());
    }

    [Fact]
    public async Task ProviderRejection_IsLoggedAndNotThrown()
    {
        var handler = new RecordingHandler(HttpStatusCode.BadRequest, """{"error":{"code":131026,"message":"Message undeliverable"}}""");

        var sent = await NewClient(handler).SendAsync(new WhatsAppNotification("+573001234567", "Aviso", "Mensaje"), TestContext.Current.CancellationToken);

        Assert.False(sent);
    }

    [Theory]
    [InlineData("Hola", 10, "Hola")]
    [InlineData("  línea 1\n\tlínea 2     fin  ", 100, "línea 1 línea 2 fin")]
    [InlineData("abcdefghijk", 6, "abcde…")]
    public void Parameter_IsCompactedAndBounded(string value, int maxLength, string expected) =>
        Assert.Equal(expected, WhatsAppCloudClient.Parameter(value, maxLength));

    [Theory]
    [InlineData("""{"error":{"code":190,"message":"Invalid OAuth access token"}}""", "190 Invalid OAuth access token")]
    [InlineData("<html>bad gateway</html>", "sin detalle")]
    [InlineData("""{"error":"texto"}""", "sin detalle")]
    public async Task ErrorSummary_KeepsOnlyCodeAndMessage(string body, string expected)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };

        Assert.Equal(expected, await WhatsAppCloudClient.ErrorSummaryAsync(response, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void WithoutCredentials_IsNotConfigured() =>
        Assert.False(new WhatsAppCloudClient(new SingleClientFactory(new RecordingHandler(HttpStatusCode.OK, "{}")),
            Options.Create(new MessagingSettings()), NullLogger<WhatsAppCloudClient>.Instance).IsConfigured);

    private static WhatsAppCloudClient NewClient(RecordingHandler handler) =>
        new(new SingleClientFactory(handler), Options.Create(Settings), NullLogger<WhatsAppCloudClient>.Instance);

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }
}
