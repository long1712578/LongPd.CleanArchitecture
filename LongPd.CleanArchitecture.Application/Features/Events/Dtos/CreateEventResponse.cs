using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongPd.CleanArchitecture.Application.Features.Events.Dtos
{
    /// <summary>Response DTO — only essential fields returned on creation.</summary>
    public sealed record CreateEventResponse(Guid Id, string Name, bool IsPublished);

}
