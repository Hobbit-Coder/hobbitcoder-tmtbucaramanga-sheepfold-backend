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

/// <summary>
/// Almacenamiento S3 privado para los archivos de los usuarios (RT-02, sección <see cref="SectionName"/>). Pensado para
/// Contabo Object Storage: endpoint https de su región, direccionamiento por ruta y credenciales por ambiente.
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Endpoint https, por ejemplo <c>https://usc1.contabostorage.com</c> (el de la región contratada).</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>Región con la que se firman las peticiones, la que indique el proveedor para ese endpoint.</summary>
    public string Region { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Contabo usa direccionamiento por ruta (<c>endpoint/bucket/clave</c>).</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Tamaño máximo por archivo. El requerimiento no lo define: por defecto, el límite de cuerpo de Kestrel
    /// (30 000 000 bytes). Contrato pendiente.
    /// </summary>
    public long MaxUploadBytes { get; set; } = 30_000_000;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceUrl) && !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
}

/// <summary>
/// Pasarela HTTP de WhatsApp para las notificaciones automáticas (sección <see cref="SectionName"/>). Mismo contrato
/// que la pasarela de seguridad de Identity; el proveedor no está contratado.
/// </summary>
public class MessagingSettings
{
    public const string SectionName = "Messaging";

    public string? GatewayUrl { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 10;
}
