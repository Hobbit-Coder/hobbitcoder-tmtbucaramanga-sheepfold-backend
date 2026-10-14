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
/// WhatsApp Cloud API de Meta para las notificaciones automáticas (sección <see cref="SectionName"/>). Misma cuenta
/// que los códigos de seguridad de Identity. Sin <see cref="PhoneNumberId"/> y <see cref="AccessToken"/> solo hay
/// avisos dentro de la aplicación.
/// </summary>
public class MessagingSettings
{
    public const string SectionName = "Messaging";

    /// <summary>Raíz https de la Graph API.</summary>
    public string BaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>Versión de la Graph API (Meta retira cada versión unos dos años después de publicarla).</summary>
    public string ApiVersion { get; set; } = "v24.0";

    /// <summary>Identificador del número de WhatsApp Business que envía los mensajes.</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Token de acceso permanente de un usuario del sistema de Meta. Solo por secretos.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Plantilla aprobada de categoría "Utilidad" con dos variables: título y mensaje del aviso.</summary>
    public string NotificationTemplate { get; set; } = "tmt_aviso";

    /// <summary>Idioma con el que se aprobó la plantilla.</summary>
    public string TemplateLanguage { get; set; } = "es";

    public int TimeoutSeconds { get; set; } = 10;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
