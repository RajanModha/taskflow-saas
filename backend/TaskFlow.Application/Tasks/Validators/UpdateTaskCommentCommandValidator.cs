using FluentValidation;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class UpdateTaskCommentCommandValidator : AbstractValidator<UpdateTaskCommentCommand>
{
    public UpdateTaskCommentCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Content)
            .NotNull()
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Content cannot be whitespace only.")
            .Must(s =>
            {
                var t = s.Trim();
                return t.Length is >= 1 and <= 4000;
            })
            .WithMessage("Content must be between 1 and 4000 characters.");
    }
}
