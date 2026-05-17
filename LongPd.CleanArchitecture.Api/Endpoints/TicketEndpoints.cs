using LongPd.CleanArchitecture.Api.Extensions;
using LongPd.CleanArchitecture.Application.Features.Tickets.Commands.ReserveTicket;
using LongPd.CleanArchitecture.Application.Features.Tickets.Commands.CancelTicket;
using LongPd.CleanArchitecture.Application.Features.Tickets.Queries.GetAvailableTickets;
using MediatR;
using LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;

namespace LongPd.CleanArchitecture.Api.Endpoints;

/// <summary>
/// REST endpoints for the Tickets feature.
/// ReserveTicket is the HOT PATH — keeps this endpoint as lean as possible.
/// </summary>
public sealed class TicketEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .WithOpenApi();

        group.MapGet("/available/{eventId:guid}", GetAvailableTicketsAsync)
            .WithName("GetAvailableTickets")
            .WithSummary("Get all available ticket tiers for an event.")
            .Produces<IReadOnlyList<AvailableTicketResponse>>(StatusCodes.Status200OK);

        group.MapPost("/reserve", ReserveTicketAsync)
            .WithName("ReserveTicket")
            .WithSummary("Reserve tickets — Flash Sale hot path with optimistic concurrency.")
            .Produces<ReserveTicketResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/cancel", CancelTicketAsync)
            .WithName("CancelTicket")
            .WithSummary("Cancel a reservation and return tickets to the pool.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    // ─── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> GetAvailableTicketsAsync(
        Guid eventId,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetAvailableTicketsQuery(eventId);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReserveTicketAsync(
        ReserveTicketRequest request,
        ISender sender,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        var userId = httpContextAccessor.HttpContext?.User
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";

        var command = new ReserveTicketCommand(request.TicketId, request.Count, userId);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelTicketAsync(
        CancelTicketRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CancelTicketCommand(request.TicketId, request.Count);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────
public sealed record ReserveTicketRequest(Guid TicketId, int Count);
public sealed record CancelTicketRequest(Guid TicketId, int Count);
