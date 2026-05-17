using Dapper;
using LongPd.CleanArchitecture.Application.Abstractions.Data;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Application.Features.Tickets.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Queries.GetAvailableTickets;

/// <summary>
/// Read-optimized query handler using Dapper.
/// Runs a single fast SQL query — no EF Core overhead, no tracking.
/// </summary>
public sealed class GetAvailableTicketsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetAvailableTicketsQuery, IReadOnlyList<AvailableTicketResponse>>
{
    public async Task<Result<IReadOnlyList<AvailableTicketResponse>>> Handle(
        GetAvailableTicketsQuery query,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                t."Id",
                t."EventId",
                t."TierName",
                t."PriceAmount"       AS "Price",
                t."PriceCurrency"     AS "Currency",
                t."AvailableQuantity",
                CASE WHEN t."AvailableQuantity" > 0 AND t."Status" = 0 THEN true ELSE false END AS "IsAvailable"
            FROM "Tickets" t
            WHERE t."EventId" = @EventId
              AND t."IsDeleted" = false
            ORDER BY t."PriceAmount" ASC
            """;

        using var connection = dbConnectionFactory.Create();

        var results = await connection.QueryAsync<AvailableTicketResponse>(
            sql,
            param: new { query.EventId });

        return Result.Success<IReadOnlyList<AvailableTicketResponse>>(results.ToList());
    }
}
