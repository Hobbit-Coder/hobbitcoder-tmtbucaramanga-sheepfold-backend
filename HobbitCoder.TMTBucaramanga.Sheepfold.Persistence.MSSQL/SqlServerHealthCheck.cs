using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.MSSQL;

public class SqlServerHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public SqlServerHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString(PersistenceConstants.ConnectionStringName);
            ArgumentNullException.ThrowIfNull(connectionString);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("SQL Server connection is healthy.");
        }
        catch (ArgumentNullException ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server connection string is not configured.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server connection is unhealthy.", ex);
        }
    }
}
