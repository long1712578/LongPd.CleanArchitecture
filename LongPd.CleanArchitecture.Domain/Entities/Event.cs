using LongPd.CleanArchitecture.Domain.Common;
using LongPd.CleanArchitecture.Domain.Exceptions;
using LongPd.CleanArchitecture.Domain.Events;
using LongPd.CleanArchitecture.Domain.ValueObjects;

namespace LongPd.CleanArchitecture.Domain.Entities;

/// <summary>
/// Aggregate root representing an event/show that has tickets.
/// All mutations go through domain methods — NO public setters.
/// </summary>
public sealed class Event : AuditableEntity, ISoftDelete
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string Venue { get; private set; } = default!;
    public int TotalCapacity { get; private set; }
    public bool IsPublished { get; private set; }

    // ISoftDelete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    private readonly List<Ticket> _tickets = [];
    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

    private Event() { }

    /// <summary>
    /// Creates a new Event. Enforces all invariants at construction time.
    /// Raises EventCreatedDomainEvent.
    /// </summary>
    public static Event Create(
        string name,
        string description,
        DateTime startDate,
        DateTime endDate,
        string venue,
        int totalCapacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Event name cannot be empty.");

        if (startDate >= endDate)
            throw new DomainException("Event start date must be before end date.");

        if (startDate <= DateTime.UtcNow)
            throw new DomainException("Event start date must be in the future.");

        if (totalCapacity <= 0)
            throw new DomainException("Event total capacity must be positive.");

        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Venue = venue.Trim(),
            TotalCapacity = totalCapacity,
            IsPublished = false
        };

        @event.RaiseDomainEvent(new EventCreatedDomainEvent(@event.Id, name));
        return @event;
    }

    public void AddTicketTier(string tierName, Money price, int quantity)
    {
        if (IsPublished)
            throw new DomainException("Cannot add tickets to a published event.");

        var totalCurrentCapacity = _tickets.Sum(t => t.TotalQuantity);
        if (totalCurrentCapacity + quantity > TotalCapacity)
            throw new DomainException("Total tickets cannot exceed event capacity.");

        var ticket = Ticket.Create(Id, tierName, price, quantity);
        _tickets.Add(ticket);
    }

    public void Publish()
    {
        if (IsPublished)
            throw new DomainException("Event is already published.");

        if (_tickets.Count == 0)
            throw new DomainException("Cannot publish an event with no tickets.");

        IsPublished = true;
        RaiseDomainEvent(new EventPublishedDomainEvent(Id));
    }

    public void UpdateDetails(string name, string description, string venue)
    {
        if (IsPublished)
            throw new DomainException("Cannot update a published event. Cancel it first.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Event name cannot be empty.");

        Name = name.Trim();
        Description = description.Trim();
        Venue = venue.Trim();
    }

    public void MarkAsDeleted(DateTime deletedAt, string? deletedBy)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }
}
