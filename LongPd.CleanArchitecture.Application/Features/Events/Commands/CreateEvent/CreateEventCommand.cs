using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Command to create a new event (draft state).
/// Validated by CreateEventCommandValidator before reaching the handler.
/// </summary>
public sealed record CreateEventCommand(
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Venue,
    int TotalCapacity) : ICommand<CreateEventResponse>;
