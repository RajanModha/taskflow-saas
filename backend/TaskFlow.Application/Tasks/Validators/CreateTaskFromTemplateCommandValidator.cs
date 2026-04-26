using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class CreateTaskFromTemplateCommandValidator : AbstractValidator<CreateTaskFromTemplateCommand>
{
    public CreateTaskFromTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();

        When(x => x.Overrides is not null, () =>
        {
            RuleFor(x => x.Overrides!.Title)
                .NotEmpty()
                .MaximumLength(200)
                .When(x => x.Overrides!.Title is not null);

            RuleFor(x => x.Overrides!.Description)
                .MaximumLength(4000)
                .When(x => x.Overrides!.Description is not null);

            RuleFor(x => x.Overrides!.Priority)
                .IsInEnum()
                .When(x => x.Overrides!.Priority.HasValue);
        });
    }
}
