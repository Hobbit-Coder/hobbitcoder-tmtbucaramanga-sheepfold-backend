namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// Acceso al directorio de cuentas de Identity con credenciales de servicio (sección <see cref="SectionName"/>).
/// El secreto es el mismo registrado en <c>Jwt:ServiceClients</c> de Identity y nunca va en el repositorio.
/// </summary>
public class IdentityDirectorySettings
{
    public const string SectionName = "IdentityDirectory";

    /// <summary>URL base de la API de Identity (normalmente la misma que <c>JwtSettings:Authority</c>).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string ClientId { get; set; } = "sheepfold-api";
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Segundos que se reutiliza el directorio antes de volver a pedirlo.</summary>
    public int CacheSeconds { get; set; } = 60;
}
