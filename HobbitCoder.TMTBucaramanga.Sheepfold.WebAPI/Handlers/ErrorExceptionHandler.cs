using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Handlers;

/// <summary>
/// Traduce las excepciones no controladas a respuestas <see cref="ProblemDetails"/> con el código HTTP
/// que corresponde. El detalle técnico (tipo, stack trace) solo se incluye en desarrollo.
/// </summary>
/// <remarks>
/// El manejador se busca recorriendo la jerarquía de la excepción, así que una subclase usa el de su
/// excepción base. Los conflictos de negocio se lanzan como <see cref="ConflictException"/>;
/// <see cref="InvalidOperationException"/> no se traduce a 409 porque EF Core y el framework la usan
/// para errores internos, que deben responder 500.
/// </remarks>
public class ErrorExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ErrorExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public ErrorExceptionHandler(ILogger<ErrorExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    private Dictionary<Type, Func<HttpContext, Exception, Task>>? _exceptionHandlers;

    private Dictionary<Type, Func<HttpContext, Exception, Task>> ExceptionHandlers => _exceptionHandlers ??= new()
    {
        { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessExceptionAsync },
        { typeof(NotFoundException), HandleNotFoundExceptionAsync },
        { typeof(ValidationException), HandleValidationExceptionAsync },
        { typeof(ConflictException), HandleConflictExceptionAsync },
        { typeof(ForbiddenAccessException), HandleForbiddenAccessExceptionAsync },
        { typeof(ExternalServiceException), HandleExternalServiceExceptionAsync },
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        for (var exceptionType = exception.GetType(); exceptionType is not null; exceptionType = exceptionType.BaseType)
        {
            if (ExceptionHandlers.TryGetValue(exceptionType, out var handler))
            {
                await handler.Invoke(httpContext, exception);
                return true;
            }
        }

        await HandleGenericExceptionAsync(httpContext, exception);
        return true;
    }

    private async Task HandleUnauthorizedAccessExceptionAsync(HttpContext httpContext, Exception ex)
    {
        _logger.LogWarning(ex,
            "Unauthorized access attempt. TraceId: {TraceId}, Path: {Path}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Detail = "Authentication is required to access this resource.",
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, httpContext.RequestAborted);
    }

    private async Task HandleNotFoundExceptionAsync(HttpContext httpContext, Exception ex)
    {
        var exception = (NotFoundException)ex;

        _logger.LogWarning(exception,
            "Resource not found. TraceId: {TraceId}, Message: {Message}",
            httpContext.TraceIdentifier,
            exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Detail = exception.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Status = StatusCodes.Status404NotFound,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, httpContext.RequestAborted);
    }

    /// <summary>Sin permiso: 403 con un mensaje genérico, sin datos del recurso.</summary>
    private async Task HandleForbiddenAccessExceptionAsync(HttpContext httpContext, Exception ex)
    {
        _logger.LogWarning(ex,
            "Forbidden. TraceId: {TraceId}, Path: {Path}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Detail = ex.Message,
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, httpContext.RequestAborted);
    }

    /// <summary>Un servicio externo (Identity, almacenamiento, mensajería) no está disponible: 503 sin detalle técnico.</summary>
    private async Task HandleExternalServiceExceptionAsync(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(ex,
            "External service unavailable. TraceId: {TraceId}, Path: {Path}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Detail = ex.Message,
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Service Unavailable",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, httpContext.RequestAborted);
    }

    private async Task HandleConflictExceptionAsync(HttpContext httpContext, Exception ex)
    {
        _logger.LogWarning(ex,
            "Business conflict. TraceId: {TraceId}, Path: {Path}, Message: {Message}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path,
            ex.Message);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Detail = ex.Message,
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, httpContext.RequestAborted);
    }

    private async Task HandleValidationExceptionAsync(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        _logger.LogWarning(exception,
            "Validation failed. TraceId: {TraceId}, Path: {Path}, ErrorCount: {ErrorCount}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path,
            exception.Errors.Count);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors.ToDictionary(error => error.Key, error => error.Value))
        {
            Instance = httpContext.Request.Path,
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, httpContext.RequestAborted);
    }

    private async Task HandleGenericExceptionAsync(HttpContext httpContext, Exception ex)
    {
        var (statusCode, title, detail) = ex switch
        {
            ArgumentException =>
                (StatusCodes.Status400BadRequest, "Bad Request", "The request contains invalid arguments."),
            TimeoutException =>
                (StatusCodes.Status408RequestTimeout, "Request Timeout", "The request has timed out."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error",
                 _environment.IsDevelopment()
                     ? ex.Message
                     : "An error occurred while processing your request.")
        };

        _logger.LogError(ex,
            "Unhandled exception occurred. TraceId: {TraceId}, ExceptionType: {ExceptionType}, Message: {Message}",
            httpContext.TraceIdentifier,
            ex.GetType().Name,
            ex.Message);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Detail = detail,
            Status = statusCode,
            Title = title,
            Type = $"https://tools.ietf.org/html/rfc7231#section-{(statusCode >= 500 ? "6.6" : "6.5")}.{statusCode % 100}",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = ex.GetType().FullName;
            problemDetails.Extensions["stackTrace"] = ex.StackTrace;

            if (ex.InnerException is not null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    message = ex.InnerException.Message,
                    type = ex.InnerException.GetType().FullName
                };
            }
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, httpContext.RequestAborted);
    }
}
