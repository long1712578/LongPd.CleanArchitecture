using LongPd.CleanArchitecture.Application.Abstractions.Messaging;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.ReserveTicket;

/// <summary>
/// Command to reserve tickets — the CORE hot path of the Flash Sale system.
/// Processed with optimistic concurrency via Ticket.RowVersion.
/// </summary>
public sealed record ReserveTicketCommand(
    Guid TicketId,
    int Count,
    string UserId) : ICommand<ReserveTicketResponse>;

public sealed record ReserveTicketResponse(
    Guid TicketId,
    Guid EventId,
    int ReservedCount,
    int RemainingQuantity,
    decimal TotalPrice,
    string Currency);
