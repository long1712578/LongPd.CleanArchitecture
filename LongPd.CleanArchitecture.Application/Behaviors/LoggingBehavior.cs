using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LongPd.CleanArchitecture.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — logs request start, completion, and elapsed time.
/// Runs FIRST in the pipeline (wraps everything else).
/// Useful for performance monitoring and debugging.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation(
            "[MediatR] Handling {RequestName} | Data: {@Request}",
            requestName, request);

        try
        {
            var response = await next();

            sw.Stop();
            logger.LogInformation(
                "[MediatR] Handled {RequestName} in {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "[MediatR] Error handling {RequestName} after {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
