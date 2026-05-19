using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Domain.Exceptions;
using LongPd.CleanArchitecture.Domain.Interfaces;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.DeleteEvent;

public sealed class DeleteEventCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteEventCommand>
{
    public async Task<Result> Handle(DeleteEventCommand request, CancellationToken ct)
    {
        var @event = await unitOfWork.Events.GetByIdAsync(request.EventId, ct);
        
        if (@event is null)
            return Result.Failure(new Error("Event.NotFound", "Event not found."));

        if (@event.IsPublished)
            return Result.Failure(new Error("Event.DomainError", "Cannot delete a published event."));

        // Use soft delete mechanism
        @event.MarkAsDeleted(DateTime.UtcNow, "System");
        unitOfWork.Events.Update(@event);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
