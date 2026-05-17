using FluentValidation;

namespace LongPd.CleanArchitecture.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Validates CreateEventCommand at the Application boundary (before domain logic).
/// Catches format/range issues early without hitting the database.
/// Domain invariants (e.g., date conflicts) are still enforced inside the entity.
/// </summary>
public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required.")
            .MaximumLength(200).WithMessage("Event name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Event description is required.")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.Venue)
            .NotEmpty().WithMessage("Venue is required.")
            .MaximumLength(500).WithMessage("Venue cannot exceed 500 characters.");

        RuleFor(x => x.TotalCapacity)
            .GreaterThan(0).WithMessage("Total capacity must be at least 1.")
            .LessThanOrEqualTo(100_000).WithMessage("Total capacity cannot exceed 100,000.");
    }
}
