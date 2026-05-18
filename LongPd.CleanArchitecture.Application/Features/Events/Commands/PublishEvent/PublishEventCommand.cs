using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.PublishEvent;

/// <summary>
/// Command to publish an event and add ticket tiers.
/// Once published, the event is open for ticket sales.
/// </summary>
public sealed record PublishEventCommand(Guid EventId, List<TicketTierRequest> TicketTiers) : ICommand;
