using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class UpdateChecklistItemCommandValidator : AbstractValidator<UpdateChecklistItemCommand>
{
    public UpdateChecklistItemCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x)
            .Must(c => c.Title is not null || c.IsCompleted is not null)
            .WithMessage("Provide at least one of title or isCompleted.");

        When(
            x => x.Title is not null,
            () =>
            {
                RuleFor(x => x.Title!)
                    .Must(t => t.Trim().Length >= 1)
                    .WithMessage("Title must be between 1 and 200 characters when provided.")
                    .MaximumLength(200);
            });
    }
}
