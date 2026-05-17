using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Events.Queries.GetEventById;

/// <summary>
/// Query to get an event by its ID.
/// Implements ICacheableQuery to opt-in to CachingBehavior
/// Cache key is unique per event ID.
/// </summary>
public sealed record GetEventByIdQuery(Guid EventId) : IQuery<EventDetailResponse>, ICacheableQuery
{
    public string CacheKey => $"event:getbyid:{EventId}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}
