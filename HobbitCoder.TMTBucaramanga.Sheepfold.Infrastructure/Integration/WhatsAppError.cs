using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// Resume el error del proveedor para el log sin el cuerpo completo: <c>error.code</c> y <c>error.message</c> (Meta) o
/// <c>response.message</c> (Evolution API).
/// </summary>
public static class WhatsAppError
{
    private const string NoDetail = "sin detalle";

    public static async Task<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return NoDetail;
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
            {
                return $"{Property(error, "code")} {Property(error, "message")}";
            }

            if (root.TryGetProperty("response", out var detail) && detail.ValueKind == JsonValueKind.Object
                && detail.TryGetProperty("message", out var message))
            {
                return message.ValueKind == JsonValueKind.Array
                    ? string.Join("; ", message.EnumerateArray().Select(item => item.ToString()))
                    : message.ToString();
            }

            return NoDetail;
        }
        catch (JsonException)
        {
            return NoDetail;
        }
    }

    private static string Property(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
}
