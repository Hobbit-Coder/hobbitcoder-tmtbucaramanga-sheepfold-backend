using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendance.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Behaviors;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application;

public static partial class Configuration
{
    /// <summary>
    ///     Registra los servicios de la capa de aplicación.
    /// </summary>
    /// <remarks>
    ///     Servicios por petición que comparten reglas entre módulos.
    /// </remarks>
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddMediator(Assembly.GetExecutingAssembly());

        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.Services.AddScoped<MinistryAccess>();
        builder.Services.AddScoped<AuditTrail>();
        builder.Services.AddScoped<NotificationDispatcher>();
        builder.Services.AddScoped<MembershipService>();
        builder.Services.AddScoped<EventAudienceResolver>();
        builder.Services.AddScoped<EventOptionsService>();
        builder.Services.AddScoped<FileStorageService>();
        builder.Services.AddScoped<AttendanceSheetService>();

        return builder;
    }
}
