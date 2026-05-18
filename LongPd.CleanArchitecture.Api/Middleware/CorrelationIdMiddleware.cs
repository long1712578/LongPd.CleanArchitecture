namespace LongPd.CleanArchitecture.Api.Middleware;

/// <summary>
/// Middleware that generates or propagates a Correlation ID for every HTTP request.
/// If the client sends an X-Correlation-Id header, it's used; otherwise, a new one is generated.
/// The Correlation ID is:
///   - Added to the response headers (for client-side tracing).
///   - Pushed into the logging scope (for structured log correlation).
///   - Available via HttpContext.TraceIdentifier for downstream services.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // Use client-provided Correlation ID or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // Set on the HttpContext for downstream access
        context.TraceIdentifier = correlationId;

        // Add to response headers so the client can track it
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Push into logging scope — all logs within this request will include the Correlation ID
        using (context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger<CorrelationIdMiddleware>()
            .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
