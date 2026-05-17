namespace LongPd.CleanArchitecture.Domain.Enums;

/// <summary>
/// Lifecycle states of a ticket slot.
/// State transitions enforced inside the Ticket entity, not externally.
/// </summary>
public enum TicketStatus
{
    /// <summary>Ticket is available for reservation.</summary>
    Available = 0,

    /// <summary>Ticket is temporarily locked during checkout (e.g., 10-minute hold).</summary>
    Reserved = 1,

    /// <summary>Payment confirmed — ticket is sold.</summary>
    Sold = 2,

    /// <summary>Ticket was cancelled and returned to available pool.</summary>
    Cancelled = 3
}
