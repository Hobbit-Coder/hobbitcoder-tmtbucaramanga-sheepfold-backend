using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Models;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Validators;

/// <summary>
/// Valida <see cref="CorsSettings"/> al arrancar. Un origen mal escrito no da ningún error: el navegador
/// simplemente bloquea las llamadas del frontend. Por eso se exige cada origen en la forma exacta que
/// envía el navegador en la cabecera <c>Origin</c>: <c>esquema://host[:puerto]</c>, sin barra final ni ruta.
/// </summary>
public class CorsSettingsValidator : IValidateOptions<CorsSettings>
{
    private const string Section = CorsSettings.SectionName;
    private const string AnyOrigin = "*";

    private readonly IHostEnvironment _environment;

    public CorsSettingsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, CorsSettings options)
    {
        var failures = new List<string>();

        foreach (var origin in options.AllowedOrigins)
        {
            ValidateOrigin(origin, failures);
        }

        if (options.MaxAgeInSeconds < 0)
        {
            failures.Add($"{Section}:MaxAgeInSeconds no puede ser negativo.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateOrigin(string origin, List<string> failures)
    {
        const string key = $"{Section}:AllowedOrigins";

        if (origin == AnyOrigin)
        {
            failures.Add($"{key} no admite '{AnyOrigin}'. Declara cada origen permitido.");
            return;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            failures.Add($"{key}: '{origin}' debe ser una URL absoluta http(s), por ejemplo https://sheepfold.tmtbucaramanga.com.");
            return;
        }

        if (!string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{key}: '{origin}' debe ser solo esquema, host y puerto, sin barra final, ruta ni query " +
                $"('{uri.GetLeftPart(UriPartial.Authority)}').");
        }

        if (uri.Scheme != Uri.UriSchemeHttps && !_environment.IsDevelopment())
        {
            failures.Add($"{key}: '{origin}' debe usar https fuera de desarrollo.");
        }
    }
}
