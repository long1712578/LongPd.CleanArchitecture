using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongPd.CleanArchitecture.Application.Features.Tickets.Dtos
{
    public sealed record AvailableTicketResponse(
      Guid Id,
      Guid EventId,
      string TierName,
      decimal Price,
      string Currency,
      int AvailableQuantity,
      bool IsAvailable);
}
