using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL;

public static class Configuration
{
    public static IHostApplicationBuilder AddPostgreSQLPersistence(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql", HealthStatus.Unhealthy);

        builder.AddInfrastructure();

        builder.Services.AddDbContext<SheepfoldContext>((serviceProvider, options) =>
            options.UseNpgsql(builder.Configuration.GetConnectionString(PersistenceConstants.ConnectionStringName), npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(Configuration).Assembly.FullName);
                npgsqlOptions.MigrationsHistoryTable(PersistenceConstants.MigrationsHistoryTable, PersistenceConstants.Schema);
                npgsqlOptions.EnableRetryOnFailure(3);
            })
            .AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>()));

        return builder;
    }
}
