namespace LongPd.CleanArchitecture.Domain.Enums;

/// <summary>
/// Lifecycle states of a ticket slot.
/// State transitions enforced inside the Ticket entity, not externally.
/// </summary>
public enum TicketStatus
{
    Available = 0,

    Reserved = 1,

    Sold = 2,

    Cancelled = 3
}
