using LongPd.CleanArchitecture.Application.Abstractions.Messaging;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.CancelTicket;

/// <summary>
/// Command to cancel a reservation and return tickets to the available pool.
/// </summary>
public sealed record CancelTicketCommand(Guid TicketId, int Count) : ICommand;
