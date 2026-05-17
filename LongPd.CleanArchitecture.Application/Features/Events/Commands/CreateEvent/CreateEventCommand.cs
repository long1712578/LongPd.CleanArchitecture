using LongPd.CleanArchitecture.Application.Abstractions.Messaging;

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

/// <summary>Response DTO — only essential fields returned on creation.</summary>
public sealed record CreateEventResponse(Guid Id, string Name, bool IsPublished);
