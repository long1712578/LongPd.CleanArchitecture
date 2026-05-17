using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongPd.CleanArchitecture.Application.Features.Events.Dtos
{
    public sealed record TicketTierSummary(
    Guid Id,
    string TierName,
    decimal Price,
    string Currency,
    int TotalQuantity,
    int AvailableQuantity,
    string Status);
}
