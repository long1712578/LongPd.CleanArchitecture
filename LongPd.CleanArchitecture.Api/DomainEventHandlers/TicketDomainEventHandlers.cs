using LongPd.CleanArchitecture.Api.Grpc;
using LongPd.CleanArchitecture.Domain.Events;
using MediatR;

namespace LongPd.CleanArchitecture.Api.DomainEventHandlers;

/// <summary>
/// Handles TicketReservedDomainEvent — bridges domain events to gRPC streaming.
/// When a ticket is reserved, this handler pushes real-time updates to all
/// connected React clients streaming via gRPC-Web.
///
/// MediatR dispatches this AFTER SaveChangesAsync (from UnitOfWork).
/// </summary>
public sealed class TicketReservedDomainEventHandler(ILogger<TicketReservedDomainEventHandler> logger)
    : INotificationHandler<TicketReservedDomainEvent>
{
    public async Task Handle(TicketReservedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "[DomainEvent] TicketReserved — TicketId: {TicketId}, Remaining: {Remaining}",
            notification.TicketId, notification.RemainingQuantity);

        var update = new AvailabilityUpdate
        {
            TicketId = notification.TicketId.ToString(),
            EventId = notification.EventId.ToString(),
            TierName = string.Empty, // Will be populated by snapshot query
            AvailableQuantity = notification.RemainingQuantity,
            TotalQuantity = 0, // Populated by snapshot
            IsSoldOut = notification.RemainingQuantity == 0,
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        await TicketGrpcServiceImpl.PushAvailabilityUpdate(notification.EventId, update);
    }
}

/// <summary>
/// Handles TicketCancelledDomainEvent — pushes availability restored update to clients.
/// </summary>
public sealed class TicketCancelledDomainEventHandler(ILogger<TicketCancelledDomainEventHandler> logger)
    : INotificationHandler<TicketCancelledDomainEvent>
{
    public async Task Handle(TicketCancelledDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "[DomainEvent] TicketCancelled — TicketId: {TicketId}, Restored: {Remaining}",
            notification.TicketId, notification.RemainingQuantity);

        var update = new AvailabilityUpdate
        {
            TicketId = notification.TicketId.ToString(),
            EventId = notification.EventId.ToString(),
            TierName = string.Empty,
            AvailableQuantity = notification.RemainingQuantity,
            TotalQuantity = 0,
            IsSoldOut = false,
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        await TicketGrpcServiceImpl.PushAvailabilityUpdate(notification.EventId, update);
    }
}
