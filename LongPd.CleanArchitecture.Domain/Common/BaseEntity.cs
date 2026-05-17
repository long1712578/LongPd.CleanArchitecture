namespace LongPd.CleanArchitecture.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Encapsulates identity and domain event collection.
/// Rule: ID is always client-generated (Guid.NewGuid()) for testability and distributed systems.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Read-only snapshot of pending domain events.
    /// Events are dispatched by UnitOfWork AFTER SaveChangesAsync succeeds.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Called by UnitOfWork after events have been dispatched.
    /// Prevents double-dispatch on retry scenarios.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
