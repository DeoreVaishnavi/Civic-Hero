namespace CivicHero.Backend.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaximumCorrelationIdLength = 100;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var suppliedCorrelationId = context.Request.Headers[HeaderName].FirstOrDefault();

        var correlationId = !string.IsNullOrWhiteSpace(suppliedCorrelationId)
                            && suppliedCorrelationId.Length <= MaximumCorrelationIdLength
            ? suppliedCorrelationId
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (context.RequestServices
                   .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                   .BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = correlationId
                   }))
        {
            await _next(context);
        }
    }
}
