using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL;

public class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public PostgreSqlHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString(PersistenceConstants.ConnectionStringName);
            ArgumentNullException.ThrowIfNull(connectionString);

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";

            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL connection is healthy.");
        }
        catch (ArgumentNullException ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection string is not configured.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection is unhealthy.", ex);
        }
    }
}