using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.CancelTicket;

/// <summary>
/// Command to cancel a reservation and return tickets to the available pool.
/// Implements ICacheInvalidatingCommand to evict stale availability data after cancellation.
/// </summary>
public sealed record CancelTicketCommand(Guid TicketId, int Count)
    : ICommand, ICacheInvalidatingCommand
{
    public IReadOnlyList<string> CacheKeysToInvalidate =>
        ["ticket:available*"];
}
