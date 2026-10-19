namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// Almacenamiento S3 privado para los archivos de los usuarios (sección <see cref="SectionName"/>). Pensado para
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
    /// Tamaño máximo por archivo. Por defecto, el límite de cuerpo de Kestrel
    /// (30 000 000 bytes).
    /// </summary>
    public long MaxUploadBytes { get; set; } = 30_000_000;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceUrl) && !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
}
