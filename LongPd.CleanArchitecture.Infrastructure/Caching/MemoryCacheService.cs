using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LongPd.CleanArchitecture.Infrastructure.Caching;

/// <summary>
/// Cache service implementation using IMemoryCache (L1 only for now).
/// Ready for L2 Redis upgrade: add IDistributedCache injection and check L2 on miss.
///
/// Note: MemoryCache is per-process — does NOT share state between app instances.
/// For multi-node scenarios, upgrade to Redis L2.
/// </summary>
public sealed class MemoryCacheService(
    IMemoryCache memoryCache,
    ILogger<MemoryCacheService> logger)
    : ICacheService
{
    private static readonly Dictionary<string, HashSet<string>> PrefixIndex = new();
    private static readonly Lock PrefixLock = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (memoryCache.TryGetValue(key, out var cached) && cached is T typedValue)
        {
            logger.LogDebug("[MemoryCache] HIT: {Key}", key);
            return Task.FromResult<T?>(typedValue);
        }

        logger.LogDebug("[MemoryCache] MISS: {Key}", key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiry.HasValue)
            options.SetAbsoluteExpiration(expiry.Value);
        else
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // Default TTL

        memoryCache.Set(key, value, options);

        // Track key by prefix
        var prefix = ExtractPrefix(key);
        lock (PrefixLock)
        {
            if (!PrefixIndex.TryGetValue(prefix, out var keys))
            {
                keys = [];
                PrefixIndex[prefix] = keys;
            }
            keys.Add(key);
        }

        logger.LogDebug("[MemoryCache] SET: {Key} (TTL: {Expiry})", key, expiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        memoryCache.Remove(key);
        logger.LogDebug("[MemoryCache] REMOVED: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        lock (PrefixLock)
        {
            if (!PrefixIndex.TryGetValue(prefix, out var keys)) return Task.CompletedTask;

            foreach (var key in keys)
                memoryCache.Remove(key);

            PrefixIndex.Remove(prefix);
            logger.LogDebug("[MemoryCache] REMOVED by prefix: {Prefix} ({Count} keys)", prefix, keys.Count);
        }
        return Task.CompletedTask;
    }

    private static string ExtractPrefix(string key)
    {
        // Convention: "entity:operation:params" → prefix = "entity:operation"
        var parts = key.Split(':');
        return parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : key;
    }
}
