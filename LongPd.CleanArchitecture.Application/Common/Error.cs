namespace LongPd.CleanArchitecture.Application.Common;

/// <summary>
/// Represents a domain or application error with a machine-readable code and human-readable description.
/// Use static factory properties for typed errors (avoids magic strings).
///
/// Convention: Code format = "Entity.ProblemType" (e.g., "Ticket.NotFound", "Event.AlreadyPublished")
/// </summary>
public sealed record Error(string Code, string Description)
{
    /// <summary>Represents "no error" — used as the null-object for Result.Success().</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Generic null/not-found error for when no specific entity error exists.</summary>
    public static readonly Error NullValue = new("General.NullValue", "A null value was provided.");

    /// <summary>Validation failure — details in Description.</summary>
    public static readonly Error Validation = new("General.Validation", "A validation error occurred.");

    // ─── Ticket-specific errors ────────────────────────────────────────────────
    public static class Ticket
    {
        public static readonly Error NotFound = new("Ticket.NotFound", "The requested ticket tier was not found.");
        public static readonly Error InsufficientQuantity = new("Ticket.InsufficientQuantity", "Not enough tickets available.");
        public static readonly Error NotAvailable = new("Ticket.NotAvailable", "This ticket tier is not currently available.");
        public static readonly Error ConcurrencyConflict = new("Ticket.ConcurrencyConflict", "The ticket was modified by another request. Please retry.");
    }

    // ─── Event-specific errors ─────────────────────────────────────────────────
    public static class Event
    {
        public static readonly Error NotFound = new("Event.NotFound", "The requested event was not found.");
        public static readonly Error AlreadyPublished = new("Event.AlreadyPublished", "This event is already published.");
        public static readonly Error NoTickets = new("Event.NoTickets", "Cannot publish an event with no ticket tiers.");
        public static readonly Error InvalidDateRange = new("Event.InvalidDateRange", "Event end date must be after start date.");
    }
}
