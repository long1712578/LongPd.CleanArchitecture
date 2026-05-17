using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.ReserveTicket;

/// <summary>
/// Command to reserve tickets — the CORE hot path of the Flash Sale system.
/// Processed with optimistic concurrency via Ticket.RowVersion.
/// </summary>
public sealed record ReserveTicketCommand(
    Guid TicketId,
    int Count,
    string UserId) : ICommand<ReserveTicketResponse>;
