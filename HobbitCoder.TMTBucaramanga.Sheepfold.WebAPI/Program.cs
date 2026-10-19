using HobbitCoder.TMTBucaramanga.Sheepfold.Application;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Handlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.AddPersistence();
builder.AddApplication();
builder.AddWebAPI();

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrorExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHostedService<ScheduledNotificationsService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            entries = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data,
                exception = app.Environment.IsDevelopment() ? entry.Value.Exception?.Message : null,
                duration = entry.Value.Duration.ToString(),
                tags = entry.Value.Tags
            })
        });
        await context.Response.WriteAsync(result);
    }
});

await app.RunAsync();
