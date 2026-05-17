using FluentValidation;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.PublishEvent;

public sealed class PublishEventCommandValidator : AbstractValidator<PublishEventCommand>
{
    public PublishEventCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("EventId is required.");

        RuleFor(x => x.TicketTiers)
            .NotEmpty().WithMessage("At least one ticket tier must be provided to publish an event.");

        RuleForEach(x => x.TicketTiers).ChildRules(tier =>
        {
            tier.RuleFor(x => x.TierName).NotEmpty();
            tier.RuleFor(x => x.PriceAmount).GreaterThanOrEqualTo(0);
            tier.RuleFor(x => x.PriceCurrency).NotEmpty().Length(3);
            tier.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}
