using FluentValidation;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.ReserveTicket;

public sealed class ReserveTicketCommandValidator : AbstractValidator<ReserveTicketCommand>
{
    public ReserveTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("TicketId is required.");

        RuleFor(x => x.Count)
            .GreaterThan(0).WithMessage("Must reserve at least 1 ticket.")
            .LessThanOrEqualTo(10).WithMessage("Cannot reserve more than 10 tickets per request.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required for reservation.");
    }
}
