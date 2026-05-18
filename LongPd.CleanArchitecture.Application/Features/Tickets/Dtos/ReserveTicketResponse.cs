namespace LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;

/// <summary>
/// Response DTO returned after a successful ticket reservation.
/// </summary>
public sealed record ReserveTicketResponse(
    Guid TicketId,
    Guid EventId,
    int ReservedCount,
    int RemainingQuantity,
    decimal TotalPrice,
    string Currency);
