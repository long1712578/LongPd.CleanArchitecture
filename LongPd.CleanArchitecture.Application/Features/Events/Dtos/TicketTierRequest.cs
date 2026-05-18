namespace LongPd.CleanArchitecture.Application.Features.Events.Dtos;

/// <summary>
/// Describes a single ticket tier when publishing an event.
/// </summary>
public sealed record TicketTierRequest(
    string TierName,
    decimal PriceAmount,
    string PriceCurrency,
    int Quantity);
