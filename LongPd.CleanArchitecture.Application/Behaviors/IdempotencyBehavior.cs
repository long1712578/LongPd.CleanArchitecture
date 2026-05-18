using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LongPd.CleanArchitecture.Application.Behaviors;

/// <summary>
/// Pipeline behavior that ensures exactly-once execution for IIdempotentCommand.
/// Uses the distributed cache to track processed RequestIds.
/// Protects against network retries causing double-charges or double-reservations.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>(
    ICacheService cache,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not IIdempotentCommand idempotentCommand)
        {
            return await next();
        }

        var cacheKey = $"idempotency:{typeof(TRequest).Name}:{idempotentCommand.RequestId}";

        // 1. Check if we've already processed this request
        var processed = await cache.GetAsync<bool>(cacheKey, ct);
        if (processed)
        {
            logger.LogWarning("[Idempotency] Duplicate request blocked: {RequestName}, RequestId: {RequestId}",
                typeof(TRequest).Name, idempotentCommand.RequestId);

            // Return a conflict error if it's a Result type
            var responseType = typeof(TResponse);
            if (responseType == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(new Error("Idempotency.Conflict", "Request already processed."));
            }
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var genericType = responseType.GetGenericArguments()[0];
                var failureMethod = typeof(Result).GetMethods()
                    .First(m => m.Name == "Failure" && m.IsGenericMethod)
                    .MakeGenericMethod(genericType);
                
                return (TResponse)failureMethod.Invoke(null, [new Error("Idempotency.Conflict", "Request already processed.")])!;
            }
            
            throw new InvalidOperationException("Duplicate request blocked by idempotency check.");
        }

        // 2. Process the request
        var response = await next();

        // 3. Mark as processed ONLY if successful
        if (IsSuccessfulResult(response))
        {
            // Keep the idempotency key around for 24 hours
            await cache.SetAsync(cacheKey, true, TimeSpan.FromHours(24), ct);
        }

        return response;
    }

    private static bool IsSuccessfulResult(TResponse response)
    {
        if (response is null) return false;
        if (response is Result result) return result.IsSuccess;
        
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var isSuccessProperty = responseType.GetProperty(nameof(Result.IsSuccess));
            return isSuccessProperty?.GetValue(response) is true;
        }

        return true;
    }
}
