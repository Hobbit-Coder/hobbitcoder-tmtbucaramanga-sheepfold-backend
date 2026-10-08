using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Validators;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI;

public static partial class Configuration
{
    /// <summary>
    /// Registra la política CORS por defecto a partir de <see cref="CorsSettings"/>, que se valida al
    /// arrancar (<see cref="CorsSettingsValidator"/>). Se aplica en el pipeline con <c>app.UseCors()</c>.
    /// </summary>
    public static IHostApplicationBuilder AddCorsSettings(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IValidateOptions<CorsSettings>, CorsSettingsValidator>();
        builder.Services.AddOptions<CorsSettings>()
            .Bind(builder.Configuration.GetSection(CorsSettings.SectionName))
            .ValidateOnStart();

        builder.Services.AddCors();
        builder.Services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSettings>>((options, corsSettings) =>
                options.AddDefaultPolicy(policy => ConfigureCorsPolicy(policy, corsSettings.Value)));

        return builder;
    }

    private static void ConfigureCorsPolicy(CorsPolicyBuilder policy, CorsSettings corsSettings)
    {
        policy.WithOrigins([.. corsSettings.AllowedOrigins]);

        if (corsSettings.AllowedMethods.Count == 0)
        {
            policy.AllowAnyMethod();
        }
        else
        {
            policy.WithMethods([.. corsSettings.AllowedMethods]);
        }

        if (corsSettings.AllowedHeaders.Count == 0)
        {
            policy.AllowAnyHeader();
        }
        else
        {
            policy.WithHeaders([.. corsSettings.AllowedHeaders]);
        }

        if (corsSettings.ExposedHeaders.Count > 0)
        {
            policy.WithExposedHeaders([.. corsSettings.ExposedHeaders]);
        }

        if (corsSettings.AllowCredentials)
        {
            policy.AllowCredentials();
        }

        if (corsSettings.MaxAgeInSeconds > 0)
        {
            policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.MaxAgeInSeconds));
        }
    }
}
