using FluentValidation;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Commands.CancelTicket;

/// <summary>
/// Validates the CancelTicketCommand before it hits the handler.
/// Automatically executed by the ValidationBehavior pipeline.
/// </summary>
public sealed class CancelTicketCommandValidator : AbstractValidator<CancelTicketCommand>
{
    public CancelTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("TicketId is required.");

        RuleFor(x => x.Count)
            .GreaterThan(0).WithMessage("Cancel count must be at least 1.");
    }
}
