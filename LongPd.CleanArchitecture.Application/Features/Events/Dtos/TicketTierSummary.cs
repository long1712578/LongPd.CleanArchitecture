namespace LongPd.CleanArchitecture.Application.Features.Events.Dtos;

/// <summary>
/// Summary of a single ticket tier - used inside EventDetailResponse.
/// </summary>
public sealed record TicketTierSummary(
    Guid Id,
    string TierName,
    decimal Price,
    string Currency,
    int TotalQuantity,
    int AvailableQuantity,
    string Status);
