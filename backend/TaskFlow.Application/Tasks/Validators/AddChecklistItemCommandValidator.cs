using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class AddChecklistItemCommandValidator : AbstractValidator<AddChecklistItemCommand>
{
    public AddChecklistItemCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .Must(t => t.Trim().Length >= 1)
            .WithMessage("Title must be between 1 and 200 characters.");

        When(
            x => x.InsertAfterOrder is not null,
            () => RuleFor(x => x.InsertAfterOrder!.Value).GreaterThanOrEqualTo(0));
    }
}
