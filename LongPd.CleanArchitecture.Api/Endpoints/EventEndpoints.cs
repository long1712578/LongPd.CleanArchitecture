using LongPd.CleanArchitecture.Api.Extensions;
using LongPd.CleanArchitecture.Application.Features.Events.Commands.CreateEvent;
using LongPd.CleanArchitecture.Application.Features.Events.Commands.PublishEvent;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;
using LongPd.CleanArchitecture.Application.Features.Events.Queries.GetEventById;
using LongPd.CleanArchitecture.Application.Features.Events.Queries.GetActiveEvents;
using MediatR;

namespace LongPd.CleanArchitecture.Api.Endpoints;

/// <summary>
/// REST endpoints for the Events feature.
/// RULES:
///   - Endpoints are THIN: HTTP → MediatR → Result → HTTP
///   - No business logic here
///   - Use TypedResults for compile-time safety
/// </summary>
public sealed class EventEndpoints : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events")
            .WithTags("Events")
            .WithOpenApi();

        group.MapPost("/", CreateEventAsync)
            .WithName("CreateEvent")
            .WithSummary("Create a new event (draft state).")
            .Produces<CreateEventResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/publish", PublishEventAsync)
            .WithName("PublishEvent")
            .WithSummary("Publish an event by adding ticket tiers and making it available.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetEventByIdAsync)
            .WithName("GetEventById")
            .WithSummary("Get event details by ID, including ticket tiers.")
            .Produces<EventDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", GetActiveEventsAsync)
            .WithName("GetActiveEvents")
            .WithSummary("Get a list of all active (non-deleted) events.")
            .Produces<IReadOnlyList<ActiveEventDto>>(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", DeleteEventAsync)
            .WithName("DeleteEvent")
            .WithSummary("Soft delete a draft/unpublished event.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    // ─── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> GetActiveEventsAsync(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetActiveEventsQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteEventAsync(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new LongPd.CleanArchitecture.Application.Features.Events.Commands.DeleteEvent.DeleteEventCommand(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateEventAsync(
        CreateEventRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateEventCommand(
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Venue,
            request.TotalCapacity);

        var result = await sender.Send(command, ct);

        return result.Match(
            onSuccess: response => Results.CreatedAtRoute(
                "GetEventById",
                new { id = response.Id },
                response),
            onFailure: error => error.ToHttpError());
    }

    private static async Task<IResult> PublishEventAsync(
        PublishEventRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new PublishEventCommand(request.EventId, request.TicketTiers);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetEventByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetEventByIdQuery(id);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }
}

// ─── Request DTOs (Api layer only — NOT shared with Application) ──────────────
public sealed record CreateEventRequest(
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string Venue,
    int TotalCapacity);

public sealed record PublishEventRequest(
    Guid EventId,
    List<TicketTierRequest> TicketTiers);
