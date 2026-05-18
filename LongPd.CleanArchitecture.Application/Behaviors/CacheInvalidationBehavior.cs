using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LongPd.CleanArchitecture.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — invalidates cache entries after successful Command execution.
/// Runs AFTER the handler completes successfully.
///
/// Flow:
///   1. Check if TRequest implements ICacheInvalidatingCommand.
///   2. If YES: execute handler → on success → remove cached entries by prefix.
///   3. If NO: pass through to handler.
///
/// Only applies to write operations (Commands). Queries bypass this behavior.
/// Works with both Result and Result{T} responses.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    ICacheService cache,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Only apply if the command opts in to cache invalidation
        if (request is not ICacheInvalidatingCommand invalidatingCommand)
            return await next();

        var response = await next();

        // Only invalidate cache on successful results
        if (!IsSuccessfulResult(response))
            return response;

        foreach (var cacheKey in invalidatingCommand.CacheKeysToInvalidate)
        {
            logger.LogDebug("[CacheInvalidation] Invalidating cache key/prefix: {CacheKey}", cacheKey);

            // If the key contains a wildcard pattern (prefix), use prefix removal
            // Otherwise, remove the exact key
            if (cacheKey.EndsWith('*'))
            {
                var prefix = cacheKey.TrimEnd('*');
                await cache.RemoveByPrefixAsync(prefix, ct);
            }
            else
            {
                await cache.RemoveAsync(cacheKey, ct);
            }
        }

        return response;
    }

    private static bool IsSuccessfulResult(TResponse response)
    {
        if (response is null) return false;

        if (response is Result result)
            return result.IsSuccess;

        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var isSuccessProperty = responseType.GetProperty(nameof(Result.IsSuccess));
            return isSuccessProperty?.GetValue(response) is true;
        }

        return true;
    }
}
