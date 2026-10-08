namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence;

/// <summary>
/// Valores de persistencia compartidos por todos los proveedores de base de datos.
/// </summary>
public static class PersistenceConstants
{
    /// <summary>Nombre de la cadena de conexión: <c>ConnectionStrings:Default</c> (o <c>ConnectionStrings__Default</c> como variable de entorno).</summary>
    public const string ConnectionStringName = "Default";

    /// <summary>Clave de configuración que elige el proveedor de base de datos.</summary>
    public const string ProviderConfigurationKey = "Persistence:Provider";

    public const string Schema = "Sheepfold";

    public const string MigrationsHistoryTable = "_EFMigrationsHistory";
}
