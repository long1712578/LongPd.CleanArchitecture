using LongPd.CleanArchitecture.Domain.Common;
using LongPd.CleanArchitecture.Domain.Enums;
using LongPd.CleanArchitecture.Domain.Events;
using LongPd.CleanArchitecture.Domain.Exceptions;
using LongPd.CleanArchitecture.Domain.ValueObjects;

namespace LongPd.CleanArchitecture.Domain.Entities;

/// <summary>
/// Represents a ticket type/tier for an Event (e.g., VIP, Standard).
/// Contains the pool of available slots — NOT individual purchased tickets.
/// Optimistic concurrency via RowVersion prevents overselling under high load.
/// </summary>
public sealed class Ticket : AuditableEntity, ISoftDelete
{
    // ─── Properties ──────────────────────────────────────────────────────────
    public Guid EventId { get; private set; }
    public string TierName { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public int TotalQuantity { get; private set; }
    public int AvailableQuantity { get; private set; }
    public TicketStatus Status { get; private set; }

    /// <summary>
    /// Optimistic concurrency token — EF Core maps this to PostgreSQL xmin or a rowversion column.
    /// Prevents the "thundering herd" oversell problem.
    /// </summary>
    public uint RowVersion { get; private set; }

    // ISoftDelete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    // Navigation
    public Event Event { get; private set; } = default!;

    private Ticket() { }

    public static Ticket Create(Guid eventId, string tierName, Money price, int quantity)
    {
        if (eventId == Guid.Empty)
            throw new DomainException("EventId cannot be empty.");

        if (string.IsNullOrWhiteSpace(tierName))
            throw new DomainException("Ticket tier name cannot be empty.");

        if (quantity <= 0)
            throw new DomainException("Ticket quantity must be positive.");

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            TierName = tierName.Trim(),
            Price = price,
            TotalQuantity = quantity,
            AvailableQuantity = quantity,
            Status = TicketStatus.Available
        };

        ticket.RaiseDomainEvent(new TicketCreatedDomainEvent(ticket.Id, eventId, tierName, quantity));
        return ticket;
    }

    /// <summary>
    /// Reserves a number of tickets for a user.
    /// This is the HOT PATH — protected by optimistic concurrency (RowVersion).
    /// </summary>
    /// <param name="count">Number of tickets to reserve.</param>
    public void Reserve(int count)
    {
        if (count <= 0)
            throw new DomainException("Reservation count must be at least 1.");

        if (Status != TicketStatus.Available)
            throw new DomainException($"Ticket tier '{TierName}' is not available for reservation.");

        if (count > AvailableQuantity)
            throw new DomainException(
                $"Only {AvailableQuantity} tickets are available for '{TierName}'. Cannot reserve {count}.");

        AvailableQuantity -= count;

        if (AvailableQuantity == 0)
            Status = TicketStatus.Sold;

        RaiseDomainEvent(new TicketReservedDomainEvent(Id, EventId, TierName, count, AvailableQuantity, TotalQuantity));
    }

    /// <summary>
    /// Cancels a reservation and returns tickets to the pool.
    /// </summary>
    public void CancelReservation(int count)
    {
        if (count <= 0)
            throw new DomainException("Cancel count must be at least 1.");

        if (count + AvailableQuantity > TotalQuantity)
            throw new DomainException("Cannot return more tickets than total quantity.");

        AvailableQuantity += count;
        Status = TicketStatus.Available;

        RaiseDomainEvent(new TicketCancelledDomainEvent(Id, EventId, TierName, count, AvailableQuantity, TotalQuantity));
    }

    public void MarkAsDeleted(DateTime deletedAt, string? deletedBy)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }
}
