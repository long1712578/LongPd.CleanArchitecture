namespace LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;

/// <summary>
/// Read DTO returned by GetAvailableTicketsQuery.
/// </summary>
public sealed record AvailableTicketResponse(
    Guid Id,
    Guid EventId,
    string TierName,
    decimal Price,
    string Currency,
    int AvailableQuantity,
    bool IsAvailable);
