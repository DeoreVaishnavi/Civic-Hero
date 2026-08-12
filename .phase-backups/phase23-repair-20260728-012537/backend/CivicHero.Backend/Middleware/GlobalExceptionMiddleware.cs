using System.Net;
using System.Text.Json;
using CivicHero.Backend.Core.Exceptions;

namespace CivicHero.Backend.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was cancelled by the client.");
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, message, errors, logAsError) = MapException(exception);
            if (logAsError)
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("Request failed with {StatusCode}: {Message}", statusCode, message);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                message,
                errors,
                traceId = context.TraceIdentifier
            }, JsonOptions), context.RequestAborted);
        }
    }

    private static (int StatusCode, string Message, IReadOnlyList<string> Errors, bool LogAsError)
        MapException(Exception exception) => exception switch
        {
            CivicHero.Backend.Core.Exceptions.ValidationException validation =>
                (StatusCodes.Status400BadRequest, validation.Message, validation.Errors, false),
            UnauthorizedAccessException unauthorized =>
                (StatusCodes.Status401Unauthorized, unauthorized.Message, Array.Empty<string>(), false),
            ConflictException conflict =>
                (StatusCodes.Status409Conflict, conflict.Message, Array.Empty<string>(), false),
            NotFoundException notFound =>
                (StatusCodes.Status404NotFound, notFound.Message, Array.Empty<string>(), false),
            BusinessRuleViolationException businessRule =>
                (StatusCodes.Status422UnprocessableEntity, businessRule.Message, Array.Empty<string>(), false),
            _ =>
                ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", Array.Empty<string>(), true)
        };
}
