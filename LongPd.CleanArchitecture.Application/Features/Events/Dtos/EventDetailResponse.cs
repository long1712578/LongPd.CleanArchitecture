using LongPd.CleanArchitecture.Application.Features.Events.Queries.GetEventById;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongPd.CleanArchitecture.Application.Features.Events.Dtos
{
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
}
