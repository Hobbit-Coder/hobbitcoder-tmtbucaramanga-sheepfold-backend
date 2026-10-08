namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Models;

/// <summary>
/// Política CORS por defecto de la API, leída de la sección <see cref="SectionName"/>.
/// Sin orígenes configurados ningún navegador de otro origen puede llamar a la API.
/// No se admite el origen comodín (<c>*</c>): cada origen se declara explícitamente.
/// </summary>
/// <remarks>
/// El token de acceso de Identity viaja en la cabecera <c>Authorization</c>, no en cookies, así que
/// <see cref="AllowCredentials"/> debe quedar en <c>false</c>.
/// </remarks>
public class CorsSettings
{
    public const string SectionName = "CorsSettings";

    /// <summary>Orígenes exactos (<c>esquema://host[:puerto]</c>), sin barra final.</summary>
    public List<string> AllowedOrigins { get; set; } = [];
    public bool AllowCredentials { get; set; }

    /// <summary>Vacío permite cualquier método.</summary>
    public List<string> AllowedMethods { get; set; } = [];

    /// <summary>Vacío permite cualquier cabecera.</summary>
    public List<string> AllowedHeaders { get; set; } = [];

    /// <summary>Cabeceras de la respuesta que el frontend puede leer (por ejemplo, <c>Location</c>).</summary>
    public List<string> ExposedHeaders { get; set; } = [];
    public int MaxAgeInSeconds { get; set; }
}
