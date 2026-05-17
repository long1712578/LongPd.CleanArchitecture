using LongPd.CleanArchitecture.Domain.Common;

namespace LongPd.CleanArchitecture.Domain.Events;

// ─── Event Domain Events ──────────────────────────────────────────────────────

/// <summary>Fired when a new Event is created</summary>
public sealed record EventCreatedDomainEvent(Guid EventId, string EventName) : IDomainEvent;

/// <summary>Fired when an Event is published and open for ticket sales.</summary>
public sealed record EventPublishedDomainEvent(Guid EventId) : IDomainEvent;

/// <summary>Fired when a ticket tier is created for an event.</summary>
public sealed record TicketCreatedDomainEvent(Guid TicketId, Guid EventId, string TierName, int TotalQuantity) : IDomainEvent;

/// <summary>
/// Fired after a successful reservation.
/// Consumers: gRPC streaming service (broadcast to React clients), notification service.
/// </summary>
public sealed record TicketReservedDomainEvent(Guid TicketId, Guid EventId, int ReservedCount, int RemainingQuantity) : IDomainEvent;

/// <summary>Fired when a reservation is cancelled and tickets are returned.</summary>
public sealed record TicketCancelledDomainEvent(Guid TicketId, Guid EventId, int CancelledCount, int RemainingQuantity) : IDomainEvent;
