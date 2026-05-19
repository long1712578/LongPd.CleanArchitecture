namespace LongPd.CleanArchitecture.Application.Features.Events.Dtos;

public sealed record ActiveEventDto(
    Guid Id,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Venue,
    int TotalCapacity,
    bool IsPublished
);
