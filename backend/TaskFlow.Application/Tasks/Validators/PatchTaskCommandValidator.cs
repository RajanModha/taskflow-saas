using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class PatchTaskCommandValidator : AbstractValidator<PatchTaskCommand>
{
    public PatchTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();

        RuleFor(x => x)
            .Must(x =>
                x.HasTitle || x.HasDescription || x.HasStatus || x.HasPriority || x.HasDueDateUtc || x.HasAssigneeId)
            .WithMessage("At least one patch field is required.");

        When(x => x.HasTitle, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(200);
        });

        When(x => x.HasDescription && x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(4000);
        });
    }
}
