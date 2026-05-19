using LongPd.CleanArchitecture.Application.Abstractions.Messaging;
using LongPd.CleanArchitecture.Application.Common;
using LongPd.CleanArchitecture.Application.Features.Events.Dtos;
using LongPd.CleanArchitecture.Domain.Entities;
using LongPd.CleanArchitecture.Domain.Exceptions;
using LongPd.CleanArchitecture.Domain.Interfaces;
using LongPd.CleanArchitecture.Domain.ValueObjects;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Handles the CreateEventCommand.
/// THIN handler — delegates all business logic to the domain entity (Event.Create).
/// </summary>
public sealed class CreateEventCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<CreateEventCommand, CreateEventResponse>
{
    public async Task<Result<CreateEventResponse>> Handle(
        CreateEventCommand command,
        CancellationToken ct)
    {
        try
        {
            if (await unitOfWork.Events.ExistsByNameAsync(command.Name, ct))
            {
                throw new DomainException($"Sự kiện với tên '{command.Name}' đã tồn tại trong hệ thống. Vui lòng chọn tên khác để tránh trùng lặp.");
            }

            // All invariants enforced inside Event.Create() — handler stays thin
            var @event = Event.Create(
                command.Name,
                command.Description,
                command.StartDate,
                command.EndDate,
                command.Venue,
                command.TotalCapacity);

            await unitOfWork.Events.AddAsync(@event, ct);
            await unitOfWork.SaveChangesAsync(ct);

            var response = new CreateEventResponse(@event.Id, @event.Name, @event.IsPublished);
            return Result.Success(response);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CreateEventResponse>(
                new Error("Event.DomainError", ex.Message));
        }
    }
}
