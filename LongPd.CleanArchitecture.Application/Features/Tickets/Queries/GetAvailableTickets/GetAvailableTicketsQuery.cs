using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Queries.GetAvailableTickets;

/// <summary>
/// Query for available ticket tiers for a given event.
/// Cacheable — ticket availability data can be served from cache
/// with a short TTL (30s) to balance freshness vs. performance.
/// </summary>
public sealed record GetAvailableTicketsQuery(Guid EventId)
    : IQuery<IReadOnlyList<AvailableTicketResponse>>, ICacheableQuery
{
    public string CacheKey => $"ticket:available:{EventId}";
    public TimeSpan? Expiry => TimeSpan.FromSeconds(30); // Short TTL — availability changes frequently
}
