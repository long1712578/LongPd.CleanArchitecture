using LongPd.CleanArchitecture.Application.Abstractions.Messaging;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.PublishEvent;

public sealed record TicketTierRequest(
    string TierName,
    decimal PriceAmount,
    string PriceCurrency,
    int Quantity);

public sealed record PublishEventCommand(
    Guid EventId,
    List<TicketTierRequest> TicketTiers) : ICommand;
