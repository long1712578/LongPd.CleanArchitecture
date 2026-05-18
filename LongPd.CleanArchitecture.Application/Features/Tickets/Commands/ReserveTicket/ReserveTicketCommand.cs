using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.ReserveTicket;

/// <summary>
/// Command to reserve tickets — the CORE hot path of the Flash Sale system.
/// Processed with optimistic concurrency via Ticket.RowVersion.
/// Implements ICacheInvalidatingCommand to evict stale availability data after reservation.
/// </summary>
public sealed record ReserveTicketCommand(
    Guid RequestId,
    Guid TicketId,
    int Count,
    string UserId) : ICommand<ReserveTicketResponse>, ICacheInvalidatingCommand, IIdempotentCommand
{
    /// <summary>
    /// Invalidate all "ticket:available" cache entries after successful reservation.
    /// Note: We invalidate by prefix since we don't know the EventId at command creation time.
    /// </summary>
    public IReadOnlyList<string> CacheKeysToInvalidate =>
        ["ticket:available*"];
}
