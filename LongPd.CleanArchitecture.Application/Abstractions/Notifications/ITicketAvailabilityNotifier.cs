namespace LongPd.CleanArchitecture.Application.Abstractions.Notifications;

public interface ITicketAvailabilityNotifier
{
    /// <summary>
    /// Pushes a ticket availability update to all connected clients subscribed to the given event.
    /// </summary>
    Task NotifyAvailabilityChangedAsync(TicketAvailabilityChangedNotification notification, CancellationToken ct = default);
}

/// <summary>
/// Payload describing a change in ticket availability for a specific event.
/// </summary>
public sealed record TicketAvailabilityChangedNotification(
    Guid TicketId,
    Guid EventId,
    string TierName,
    int AvailableQuantity,
    int TotalQuantity,
    bool IsSoldOut,
    DateTime UpdatedAt);
