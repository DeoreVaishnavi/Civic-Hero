using System.Text.Json;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Middleware;

public sealed class RequestSizeGuardMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<SecurityOptions> _options;

    public RequestSizeGuardMiddleware(RequestDelegate next, IOptionsMonitor<SecurityOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequestCanContainBody(context.Request.Method) || !context.Request.ContentLength.HasValue)
        {
            await _next(context);
            return;
        }

        var options = _options.CurrentValue;
        var isUpload = IsUploadPath(context.Request.Path);
        var limitMb = isUpload ? options.UploadRequestBodyLimitMb : options.DefaultRequestBodyLimitMb;
        var limitBytes = Math.Max(1, limitMb) * 1024L * 1024L;

        if (context.Request.ContentLength.Value > limitBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                message = $"Request body exceeds the {limitMb} MB limit for this endpoint.",
                maximumBytes = limitBytes,
                traceId = context.TraceIdentifier
            }, JsonOptions), context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private static bool RequestCanContainBody(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    private static bool IsUploadPath(PathString path) =>
        path.StartsWithSegments("/api/v1/files") ||
        path.StartsWithSegments("/api/v1/complaints") ||
        path.StartsWithSegments("/api/v1/assignments");
}
