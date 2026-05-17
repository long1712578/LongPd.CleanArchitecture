using LongPd.CleanArchitecture.Domain.Entities;

namespace LongPd.CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Event-specific repository for write operations.
/// Add domain-specific query methods here (write side only).
/// </summary>
public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// Gets an event with its ticket tiers loaded.
    /// Used when publishing an event (needs to verify tickets exist).
    /// </summary>
    Task<Event?> GetByIdWithTicketsAsync(Guid eventId, CancellationToken ct = default);
}
