using LongPd.CleanArchitecture.Application.Abstractions.Notifications;
using LongPd.CleanArchitecture.Domain.Events;
using MediatR;

namespace LongPd.CleanArchitecture.Api.DomainEventHandlers;

/// <summary>
/// Handles TicketReservedDomainEvent — bridges domain events to gRPC streaming.
/// When a ticket is reserved, pushes real-time availability updates to all
/// connected React clients via gRPC-Web.
///
/// MediatR dispatches this AFTER SaveChangesAsync (from UnitOfWork).
/// </summary>
public sealed class TicketReservedDomainEventHandler(
    ITicketAvailabilityNotifier notifier,
    ILogger<TicketReservedDomainEventHandler> logger)
    : INotificationHandler<TicketReservedDomainEvent>
{
    public async Task Handle(TicketReservedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[DomainEvent] TicketReserved — TicketId: {TicketId}, Remaining: {Remaining}", notification.TicketId, notification.RemainingQuantity);

        await notifier.NotifyAvailabilityChangedAsync(new TicketAvailabilityChangedNotification(
            notification.TicketId,
            notification.EventId,
            notification.TierName,
            notification.RemainingQuantity,
            notification.TotalQuantity,
            IsSoldOut: notification.RemainingQuantity == 0,
            UpdatedAt: DateTime.UtcNow), ct);
    }
}

/// <summary>
/// Handles TicketCancelledDomainEvent — pushes availability restored update to all connected clients.
/// </summary>
public sealed class TicketCancelledDomainEventHandler(
    ITicketAvailabilityNotifier notifier,
    ILogger<TicketCancelledDomainEventHandler> logger)
    : INotificationHandler<TicketCancelledDomainEvent>
{
    public async Task Handle(TicketCancelledDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("[DomainEvent] TicketCancelled — TicketId: {TicketId}, Restored: {Remaining}", notification.TicketId, notification.RemainingQuantity);

        await notifier.NotifyAvailabilityChangedAsync(new TicketAvailabilityChangedNotification(
            notification.TicketId,
            notification.EventId,
            notification.TierName,
            notification.RemainingQuantity,
            notification.TotalQuantity,
            IsSoldOut: false,
            UpdatedAt: DateTime.UtcNow), ct);
    }
}
