using Dapper;
using LongPd.CleanArchitecture.Application.Abstractions.Data;
using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Events.Queries.GetActiveEvents;

public sealed class GetActiveEventsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetActiveEventsQuery, IReadOnlyList<ActiveEventDto>>
{
    public async Task<Result<IReadOnlyList<ActiveEventDto>>> Handle(GetActiveEventsQuery request, CancellationToken ct)
    {
        const string sql = """
            SELECT 
                "Id", 
                "Name", 
                "Description", 
                "StartDate", 
                "EndDate", 
                "Venue", 
                "TotalCapacity", 
                "IsPublished"
            FROM "Events"
            WHERE "IsDeleted" = false
            ORDER BY "CreatedAt" DESC
            LIMIT 50
            """;

        using var connection = await dbConnectionFactory.CreateAsync(ct);
        var events = await connection.QueryAsync<ActiveEventDto>(sql);

        return Result.Success<IReadOnlyList<ActiveEventDto>>(events.ToList().AsReadOnly());
    }
}
