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
///   2. If YES: try cache → handler → write-through to cache on success.
///   3. If NO: pass through to handler.
///
/// Only applies to Query paths (read-only). Commands bypass this behavior.
/// Result-aware: only caches successful Result{T} responses — never caches errors.
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

        // Only cache successful results — type-safe check without 'dynamic'
        if (IsSuccessfulResult(response))
        {
            await cache.SetAsync(cacheKey, response!, expiry, ct);
        }

        return response;
    }

    /// <summary>
    /// Type-safe check for Result success without using 'dynamic'.
    /// Handles both Result and Result{T} response types.
    /// Non-Result responses are always considered cacheable (direct DTOs).
    /// </summary>
    private static bool IsSuccessfulResult(TResponse response)
    {
        if (response is null)
            return false;

        // Check if TResponse is the non-generic Result type
        if (response is Result result)
            return result.IsSuccess;

        // Check if TResponse is Result<T> — use interface-based check via reflection-free pattern
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            // Access IsSuccess property through the known generic type structure
            var isSuccessProperty = responseType.GetProperty(nameof(Result.IsSuccess));
            return isSuccessProperty?.GetValue(response) is true;
        }

        // Non-Result responses (raw DTOs) — always cache
        return true;
    }
}

