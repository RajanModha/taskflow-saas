using FluentValidation;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Priority)
            .IsInEnum();

        When(
            x => x.TagIds is not null,
            () =>
            {
                RuleFor(x => x.TagIds!)
                    .Must(ids => ids.Length <= 50)
                    .WithMessage("At most 50 tags can be assigned to a task.");
            });
    }
}

