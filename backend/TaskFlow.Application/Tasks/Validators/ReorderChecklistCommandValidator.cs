using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class ReorderChecklistCommandValidator : AbstractValidator<ReorderChecklistCommand>
{
    public ReorderChecklistCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.OrderedIds).NotNull().NotEmpty();
        RuleFor(x => x.OrderedIds).Must(ids => ids!.Length == ids.Distinct().Count()).WithMessage("orderedIds must be unique.");
    }
}
