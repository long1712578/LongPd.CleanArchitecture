using Dapper;
using LongPd.CleanArchitecture.Application.Abstractions.Data;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Events.Queries.GetEventById;

/// <summary>
/// Reads event data using Dapper (NOT EF Core) for maximum query performance.
/// No entity tracking — this is the read-optimized path.
/// </summary>
public sealed class GetEventByIdQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetEventByIdQuery, EventDetailResponse>
{
    public async Task<Result<EventDetailResponse>> Handle(
        GetEventByIdQuery query,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                e."Id", e."Name", e."Description", e."StartDate", e."EndDate",
                e."Venue", e."TotalCapacity", e."IsPublished", e."CreatedAt", e."CreatedBy",
                t."Id"                AS "TicketId",
                t."TierName",
                t."PriceAmount"       AS "Price",
                t."PriceCurrency"     AS "Currency",
                t."TotalQuantity",
                t."AvailableQuantity",
                t."Status"
            FROM "Events" e
            LEFT JOIN "Tickets" t ON t."EventId" = e."Id" AND t."IsDeleted" = false
            WHERE e."Id" = @EventId AND e."IsDeleted" = false
            """;

        using var connection = dbConnectionFactory.Create();

        // Multi-mapping: join Event + Ticket rows
        var eventDictionary = new Dictionary<Guid, (EventRow Event, List<TicketRow> Tickets)>();

        await connection.QueryAsync<EventRow, TicketRow?, object>(
            sql,
            (eventRow, ticketRow) =>
            {
                if (!eventDictionary.TryGetValue(eventRow.Id, out var entry))
                {
                    entry = (eventRow, []);
                    eventDictionary[eventRow.Id] = entry;
                }
                if (ticketRow is not null)
                    entry.Tickets.Add(ticketRow);
                return new();
            },
            param: new { query.EventId },
            splitOn: "TicketId");

        if (!eventDictionary.TryGetValue(query.EventId, out var result))
            return Result.Failure<EventDetailResponse>(Error.Event.NotFound);

        var (ev, tickets) = result;

        var response = new EventDetailResponse(
            ev.Id, ev.Name, ev.Description, ev.StartDate, ev.EndDate,
            ev.Venue, ev.TotalCapacity, ev.IsPublished, ev.CreatedAt, ev.CreatedBy,
            tickets.Select(t => new TicketTierSummary(
                t.TicketId, t.TierName, t.Price, t.Currency,
                t.TotalQuantity, t.AvailableQuantity, t.Status)).ToList());

        return Result.Success(response);
    }

    // ─── Internal Dapper row models (NOT domain entities) ────────────────────
    private sealed record EventRow(
        Guid Id, string Name, string Description,
        DateTime StartDate, DateTime EndDate,
        string Venue, int TotalCapacity, bool IsPublished,
        DateTime CreatedAt, string? CreatedBy);

    private sealed record TicketRow(
        Guid TicketId, string TierName, decimal Price, string Currency,
        int TotalQuantity, int AvailableQuantity, string Status);
}
