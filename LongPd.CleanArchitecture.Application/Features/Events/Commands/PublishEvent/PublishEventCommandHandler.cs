using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Domain.Exceptions;
using LongPd.CleanArchitecture.Domain.Interfaces;
using LongPd.CleanArchitecture.Domain.ValueObjects;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.PublishEvent;

public sealed class PublishEventCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<PublishEventCommand>
{
    public async Task<Result> Handle(
        PublishEventCommand command,
        CancellationToken ct)
    {
        var @event = await unitOfWork.Events.GetByIdAsync(command.EventId, ct);

        if (@event is null)
            return Result.Failure(Error.Event.NotFound);

        try
        {
            foreach (var tier in command.TicketTiers)
            {
                var price = Money.Of(tier.PriceAmount, tier.PriceCurrency);
                @event.AddTicketTier(tier.TierName, price, tier.Quantity);
            }

            @event.Publish();

            await unitOfWork.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(new Error("Event.DomainError", ex.Message));
        }
    }
}
