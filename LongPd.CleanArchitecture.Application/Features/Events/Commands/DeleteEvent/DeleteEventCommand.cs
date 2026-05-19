using LongPd.CleanArchitecture.Application.Abstractions.Messaging;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.DeleteEvent;

public sealed record DeleteEventCommand(Guid EventId) : ICommand;
