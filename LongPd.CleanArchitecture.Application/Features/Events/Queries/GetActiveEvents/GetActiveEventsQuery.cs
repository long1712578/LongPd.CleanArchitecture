using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;

namespace LongPd.CleanArchitecture.Application.Features.Events.Queries.GetActiveEvents;

public sealed record GetActiveEventsQuery : IQuery<IReadOnlyList<ActiveEventDto>>;
