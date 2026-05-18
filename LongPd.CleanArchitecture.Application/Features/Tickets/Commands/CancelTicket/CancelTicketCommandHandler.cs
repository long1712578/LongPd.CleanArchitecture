using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Domain.Exceptions;
using LongPd.CleanArchitecture.Domain.Interfaces;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.CancelTicket;

/// <summary>
/// Handles CancelTicketCommand.
/// Protected by Optimistic Concurrency (RowVersion/xmin) during SaveChangesAsync.
/// UnitOfWork translates DB-level concurrency exceptions → ConcurrencyException.
/// </summary>
public sealed class CancelTicketCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<CancelTicketCommand>
{
    public async Task<Result> Handle(
        CancelTicketCommand command,
        CancellationToken ct)
    {
        var ticket = await unitOfWork.Tickets.GetByIdAsync(command.TicketId, ct);

        if (ticket is null)
            return Result.Failure(Error.Ticket.NotFound);

        try
        {
            ticket.CancelReservation(command.Count);
            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(new Error("Ticket.DomainError", ex.Message));
        }
        catch (ConcurrencyException)
        {
            return Result.Failure(Error.Ticket.ConcurrencyConflict);
        }
    }
}
