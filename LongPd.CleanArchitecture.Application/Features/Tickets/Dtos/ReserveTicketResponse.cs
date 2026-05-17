using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Dtos
{
    public sealed record ReserveTicketResponse(
    Guid TicketId,
    Guid EventId,
    int ReservedCount,
    int RemainingQuantity,
    decimal TotalPrice,
    string Currency);
}
