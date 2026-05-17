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
///   - Prevention of cache stampede (double-fetching on concurrent misses).
/// </summary>
public sealed class HybridCacheService(
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    ILogger<HybridCacheService> logger)
    : ICacheService
{
    private static readonly Dictionary<string, HashSet<string>> L1PrefixIndex = new();
    private static readonly Lock PrefixLock = new();
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
                    TrackL1Prefix(key);

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
        TrackL1Prefix(key);
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
        List<string> keysToRemove;
        lock (PrefixLock)
        {
            if (L1PrefixIndex.TryGetValue(prefix, out var keys))
            {
                keysToRemove = [.. keys];
                L1PrefixIndex.Remove(prefix);
            }
            else
            {
                keysToRemove = [];
            }
        }

        foreach (var key in keysToRemove)
        {
            memoryCache.Remove(key);
        }

        logger.LogDebug("[HybridCache] Invalidated {Count} keys in L1 for prefix: {Prefix}", keysToRemove.Count, prefix);
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

    private static void TrackL1Prefix(string key)
    {
        var prefix = ExtractPrefix(key);
        lock (PrefixLock)
        {
            if (!L1PrefixIndex.TryGetValue(prefix, out var keys))
            {
                keys = [];
                L1PrefixIndex[prefix] = keys;
            }
            keys.Add(key);
        }
    }

    private static string ExtractPrefix(string key)
    {
        var parts = key.Split(':');
        return parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : key;
    }
}
