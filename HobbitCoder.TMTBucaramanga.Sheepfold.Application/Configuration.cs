using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Behaviors;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application;

public static partial class Configuration
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddMediator(Assembly.GetExecutingAssembly());

        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return builder;
    }
}
