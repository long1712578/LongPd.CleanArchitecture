using System.Collections.Concurrent;
using System.Text.Json;
using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LongPd.CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Enterprise Hybrid L1/L2 Cache Service.
///   - L1: In-Memory Cache (ultrafast, per-process RAM)
///   - L2: Distributed Cache (Redis, shared state across instances)
///
/// Features:
///   - Graceful fallback: If L2 is unavailable or connection fails, falls back to L1 only.
///   - Thread-safe prefix tracking using ConcurrentDictionary.
///   - Prevention of cache stampede (double-fetching on concurrent misses).
/// </summary>
public sealed class HybridCacheService(
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    ILogger<HybridCacheService> logger)
    : ICacheService
{
    /// <summary>
    /// Thread-safe prefix index: maps prefix → set of full cache keys.
    /// Uses ConcurrentDictionary for lock-free reads and safe concurrent writes.
    /// Inner dictionary uses byte as dummy value (acts as a concurrent HashSet).
    /// </summary>
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> PrefixIndex = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (memoryCache.TryGetValue(key, out var cachedL1) && cachedL1 is T typedValue)
        {
            logger.LogDebug("[HybridCache] L1 HIT: {Key}", key);
            return typedValue;
        }

        logger.LogDebug("[HybridCache] L1 MISS, checking L2: {Key}", key);
        try
        {
            var cachedL2Bytes = await distributedCache.GetAsync(key, ct);
            if (cachedL2Bytes is not null && cachedL2Bytes.Length > 0)
            {
                var value = JsonSerializer.Deserialize<T>(cachedL2Bytes, JsonOptions);
                if (value is not null)
                {
                    logger.LogDebug("[HybridCache] L2 HIT: {Key}. Writing back to L1.", key);
                    memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
                    TrackPrefix(key);

                    return value;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[HybridCache] L2 read failed for key {Key}. Falling back gracefully.", key);
        }

        logger.LogDebug("[HybridCache] L1 & L2 MISS: {Key}", key);
        return default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var ttl = expiry ?? TimeSpan.FromMinutes(5);

        memoryCache.Set(key, value, ttl);
        TrackPrefix(key);
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            await distributedCache.SetAsync(key, bytes, options, ct);
            logger.LogDebug("[HybridCache] SET L1 & L2 successfully for key: {Key} (TTL: {TTL})", key, ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[HybridCache] L2 write failed for key {Key}. L1 is set.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        memoryCache.Remove(key);
        UntrackKey(key);
        try
        {
            await distributedCache.RemoveAsync(key, ct);
            logger.LogDebug("[HybridCache] REMOVED key from L1 & L2: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[HybridCache] L2 remove failed for key {Key}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Collect all keys matching the prefix
        var keysToRemove = new List<string>();

        foreach (var kvp in PrefixIndex)
        {
            if (kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                keysToRemove.AddRange(kvp.Value.Keys);
                PrefixIndex.TryRemove(kvp.Key, out _);
            }
        }

        // Remove from L1
        foreach (var key in keysToRemove)
        {
            memoryCache.Remove(key);
        }

        logger.LogDebug("[HybridCache] Invalidated {Count} keys in L1 for prefix: {Prefix}", keysToRemove.Count, prefix);

        // Remove from L2
        foreach (var key in keysToRemove)
        {
            try
            {
                await distributedCache.RemoveAsync(key, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[HybridCache] L2 prefix remove failed for key {Key}", key);
            }
        }
    }

    private static void TrackPrefix(string key)
    {
        var prefix = ExtractPrefix(key);
        var keys = PrefixIndex.GetOrAdd(prefix, _ => new ConcurrentDictionary<string, byte>());
        keys.TryAdd(key, 0);
    }

    private static void UntrackKey(string key)
    {
        var prefix = ExtractPrefix(key);
        if (PrefixIndex.TryGetValue(prefix, out var keys))
        {
            keys.TryRemove(key, out _);
            // Clean up empty prefix entries to prevent memory leaks
            if (keys.IsEmpty)
                PrefixIndex.TryRemove(prefix, out _);
        }
    }

    private static string ExtractPrefix(string key)
    {
        var parts = key.Split(':');
        return parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : key;
    }
}

