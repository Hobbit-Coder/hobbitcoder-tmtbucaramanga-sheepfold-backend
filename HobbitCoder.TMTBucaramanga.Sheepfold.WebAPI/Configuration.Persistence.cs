using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence;
using HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.MSSQL;
using HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI;

public static partial class Configuration
{
    private const string PostgreSqlProvider = "PostgreSQL";
    private const string SqlServerProvider = "SqlServer";

    /// <summary>
    /// Registra el proveedor de base de datos indicado en <c>Persistence:Provider</c>
    /// (<c>PostgreSQL</c> o <c>SqlServer</c>; PostgreSQL si no se indica).
    /// </summary>
    public static IHostApplicationBuilder AddPersistence(this IHostApplicationBuilder builder)
    {
        var provider = builder.Configuration[PersistenceConstants.ProviderConfigurationKey] ?? PostgreSqlProvider;

        if (string.Equals(provider, PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
        {
            return builder.AddPostgreSQLPersistence();
        }

        if (string.Equals(provider, SqlServerProvider, StringComparison.OrdinalIgnoreCase))
        {
            return builder.AddSqlServerPersistence();
        }

        throw new InvalidOperationException(
            $"{PersistenceConstants.ProviderConfigurationKey} '{provider}' no es válido. Valores admitidos: {PostgreSqlProvider}, {SqlServerProvider}.");
    }
}
