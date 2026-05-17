using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;
using LongPd.CleanArchitecture.Domain.Exceptions;
using LongPd.CleanArchitecture.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.ReserveTicket;

/// <summary>
/// Handles ReserveTicketCommand — the HOT PATH.
///
/// Concurrency strategy:
///   1. Fetch ticket with tracking (EF Core).
///   2. Call ticket.Reserve() — domain validates availability.
///   3. SaveChangesAsync — EF Core checks RowVersion (optimistic concurrency).
///   4. On DbUpdateConcurrencyException — return conflict error (client must retry).
///
/// This means NO pessimistic locking (no SELECT FOR UPDATE) at application level.
/// The RowVersion check at DB level prevents overselling with minimal lock contention.
/// </summary>
public sealed class ReserveTicketCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<ReserveTicketCommand, ReserveTicketResponse>
{
    public async Task<Result<ReserveTicketResponse>> Handle(
        ReserveTicketCommand command,
        CancellationToken ct)
    {
        var ticket = await unitOfWork.Tickets.GetByIdAsync(command.TicketId, ct);

        if (ticket is null)
            return Result.Failure<ReserveTicketResponse>(Error.Ticket.NotFound);

        try
        {
            // Domain entity enforces all business rules
            ticket.Reserve(command.Count);

            // EF Core detects RowVersion mismatch → throws DbUpdateConcurrencyException
            await unitOfWork.SaveChangesAsync(ct);

            var response = new ReserveTicketResponse(
                ticket.Id,
                ticket.EventId,
                command.Count,
                ticket.AvailableQuantity,
                ticket.Price.Amount * command.Count,
                ticket.Price.Currency);

            return Result.Success(response);
        }
        catch (DomainException ex)
        {
            return Result.Failure<ReserveTicketResponse>(
                new Error("Ticket.DomainError", ex.Message));
        }
        catch (DbUpdateConcurrencyException)
        {
            // Thundering herd: another request reserved tickets simultaneously
            return Result.Failure<ReserveTicketResponse>(Error.Ticket.ConcurrencyConflict);
        }
    }
}
