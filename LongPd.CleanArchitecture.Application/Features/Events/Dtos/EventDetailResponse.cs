namespace LongPd.CleanArchitecture.Application.Features.Events.Dtos;

/// <summary>
/// Full event detail including ticket tiers - returned by GetEventByIdQuery.
/// </summary>
public sealed record EventDetailResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Venue,
    int TotalCapacity,
    bool IsPublished,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<TicketTierSummary> TicketTiers);
