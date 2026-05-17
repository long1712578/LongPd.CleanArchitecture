using LongPd.CleanArchitecture.Domain.Entities;

namespace LongPd.CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Ticket-specific repository for write operations.
/// Hot-path: GetByIdForUpdateAsync used by ReserveTicketCommandHandler.
/// </summary>
public interface ITicketRepository : IRepository<Ticket>
{
    /// <summary>
    /// Fetches a ticket with pessimistic lock hint (FOR UPDATE) for reservation.
    /// Combined with EF Core optimistic concurrency (RowVersion) for double safety.
    /// </summary>
    Task<Ticket?> GetByIdWithLockAsync(Guid ticketId, CancellationToken ct = default);

    /// <summary>Gets all ticket tiers for an event (write-side, includes tracking).</summary>
    Task<List<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
}
