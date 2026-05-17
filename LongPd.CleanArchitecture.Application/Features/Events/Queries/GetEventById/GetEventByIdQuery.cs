using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;

namespace LongPd.CleanArchitecture.Application.Features.Events.Queries.GetEventById;

/// <summary>
/// Query to get an event by its ID.
/// Implements ICacheableQuery to opt-in to CachingBehavior (L1 + L2 cache).
/// Cache key is unique per event ID.
/// </summary>
public sealed record GetEventByIdQuery(Guid EventId)
    : IQuery<EventDetailResponse>, ICacheableQuery
{
    public string CacheKey => $"event:getbyid:{EventId}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Full event detail response — used for single-event views.
/// </summary>
public sealed record EventDetailResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Venue,
    int TotalCapacity,
    bool IsPublished,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<TicketTierSummary> TicketTiers);

public sealed record TicketTierSummary(
    Guid Id,
    string TierName,
    decimal Price,
    string Currency,
    int TotalQuantity,
    int AvailableQuantity,
    string Status);
