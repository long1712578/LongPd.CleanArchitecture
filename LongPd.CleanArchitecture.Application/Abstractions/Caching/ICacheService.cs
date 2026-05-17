namespace LongPd.CleanArchitecture.Application.Abstractions.Caching;

/// <summary>
/// Marker interface for queries that should be cached via CachingBehavior.
/// Implement this on IQuery records to opt-in to the caching pipeline.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Cache key — should be unique per query parameters.
    /// Convention: "entity:operation:params" (e.g., "event:getbyid:guid")
    /// </summary>
    string CacheKey { get; }

    /// <summary>Cache duration. Default: 5 minutes for most queries.</summary>
    TimeSpan? Expiry => TimeSpan.FromMinutes(5);
}

/// <summary>
/// Abstraction for the hybrid cache service (L1: IMemoryCache + L2: IDistributedCache).
/// Application layer uses this; Implementation is in Infrastructure.
/// </summary>
public interface ICacheService
{
    /// <summary>Gets a cached value by key. Returns default if not found.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Sets a cached value with a given key and expiration.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>Removes a cached value by key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Removes all cached values matching a key prefix.</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
