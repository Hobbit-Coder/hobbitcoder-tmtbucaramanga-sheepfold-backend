using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Utils;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI;

public static partial class Configuration
{
    public static IHostApplicationBuilder AddWebAPI(this IHostApplicationBuilder builder)
    {
        builder.AddCorsSettings();
        builder.AddJwtSettings();

        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        return builder;
    }
}
