using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class BulkUpdateTasksCommandValidator : AbstractValidator<BulkUpdateTasksCommand>
{
    public BulkUpdateTasksCommandValidator()
    {
        RuleFor(x => x.TaskIds)
            .NotNull()
            .Must(ids => ids.Length > 0)
            .WithMessage("At least one task id is required.")
            .Must(ids => ids.Length <= 100)
            .WithMessage("No more than 100 task ids are allowed.");

        RuleFor(x => x.Updates)
            .Must(u => u.Status is not null || u.Priority is not null || u.HasDueDateUtc || u.HasAssigneeId)
            .WithMessage("At least one update field is required.");
    }
}
