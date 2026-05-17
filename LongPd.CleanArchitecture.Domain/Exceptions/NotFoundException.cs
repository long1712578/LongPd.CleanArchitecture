using LongPd.CleanArchitecture.Domain.Entities;

namespace LongPd.CleanArchitecture.Domain.Exceptions;

/// <summary>
/// Base exception for not found errors.
/// NOT sealed so that specific entities can inherit from it (e.g., TicketNotFoundException).
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.") { }
}

public sealed class TicketNotFoundException : NotFoundException
{
    public TicketNotFoundException(Guid ticketId)
        : base(nameof(Ticket), ticketId) { }
}

public sealed class EventNotFoundException : NotFoundException
{
    public EventNotFoundException(Guid eventId)
        : base(nameof(Event), eventId) { }
}
