using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class BulkAssignTasksCommandValidator : AbstractValidator<BulkAssignTasksCommand>
{
    public BulkAssignTasksCommandValidator()
    {
        RuleFor(x => x.TaskIds)
            .NotNull()
            .Must(ids => ids.Length > 0)
            .WithMessage("At least one task id is required.")
            .Must(ids => ids.Length <= 100)
            .WithMessage("No more than 100 task ids are allowed.");
    }
}
