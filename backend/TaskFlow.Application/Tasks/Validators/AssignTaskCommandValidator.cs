using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
