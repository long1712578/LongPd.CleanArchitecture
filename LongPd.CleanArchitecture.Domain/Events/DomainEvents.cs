using LongPd.CleanArchitecture.Domain.Common;

namespace LongPd.CleanArchitecture.Domain.Events;

public sealed record EventCreatedDomainEvent(Guid EventId, string EventName) : IDomainEvent;

public sealed record EventPublishedDomainEvent(Guid EventId) : IDomainEvent;

public sealed record TicketCreatedDomainEvent(Guid TicketId, Guid EventId, string TierName, int TotalQuantity) : IDomainEvent;

/// <summary>
/// Fired after a successful reservation.
/// Consumers: gRPC streaming service (broadcast to React clients), notification service.
/// </summary>
public sealed record TicketReservedDomainEvent(
    Guid TicketId,
    Guid EventId,
    string TierName,
    int ReservedCount,
    int RemainingQuantity,
    int TotalQuantity) : IDomainEvent;

/// <summary>
/// Fired when a reservation is cancelled and tickets are returned.
/// </summary>
public sealed record TicketCancelledDomainEvent(
    Guid TicketId,
    Guid EventId,
    string TierName,
    int CancelledCount,
    int RemainingQuantity,
    int TotalQuantity) : IDomainEvent;
