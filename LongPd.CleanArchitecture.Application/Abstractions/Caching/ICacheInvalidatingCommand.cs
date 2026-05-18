namespace LongPd.CleanArchitecture.Application.Abstractions.Caching;

/// <summary>
/// Marker interface for Commands that should invalidate specific cache entries after success.
/// Implement this on ICommand records to opt-in to automatic cache invalidation.
///
/// Usage: When a write operation (Reserve/Cancel ticket) succeeds, the stale cached
/// query data must be evicted so subsequent reads get fresh data.
/// </summary>
public interface ICacheInvalidatingCommand
{
    /// <summary>
    /// Cache key prefixes to invalidate after successful command execution.
    /// Convention: "entity:operation" (e.g., "ticket:available", "event:getbyid")
    /// </summary>
    IReadOnlyList<string> CacheKeysToInvalidate { get; }
}
