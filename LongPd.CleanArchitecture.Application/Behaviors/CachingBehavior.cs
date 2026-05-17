using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LongPd.CleanArchitecture.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — intercepts cacheable queries (ICacheableQuery).
/// Runs THIRD (after LoggingBehavior + ValidationBehavior).
///
/// Flow:
///   1. Check if TRequest implements ICacheableQuery.
///   2. If YES: try L1 cache → L2 cache → handler → write-through to caches.
///   3. If NO: pass through to handler.
///
/// Only applies to Query paths (read-only). Commands bypass this behavior.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Only apply caching if the request explicitly opts in
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var cacheKey = cacheableQuery.CacheKey;
        var expiry = cacheableQuery.Expiry;

        // Try to get from cache
        var cachedValue = await cache.GetAsync<TResponse>(cacheKey, ct);
        if (cachedValue is not null)
        {
            logger.LogDebug("[Cache] HIT for key: {CacheKey}", cacheKey);
            return cachedValue;
        }

        logger.LogDebug("[Cache] MISS for key: {CacheKey} — calling handler", cacheKey);

        // Cache miss — call handler and cache the result
        var response = await next();

        // Only cache successful results (don't cache errors)
        var responseType = typeof(TResponse);
        if (responseType == typeof(Result) || responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            dynamic result = response!;
            if (result.IsSuccess)
                await cache.SetAsync(cacheKey, response!, expiry, ct);
        }

        return response;
    }
}
