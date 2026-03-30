using FluentValidation;

namespace TaskFlow.Application.Workspaces.Validation;

public sealed class CreateWorkspaceRequestValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(128)
            .Matches("^[\\p{L}0-9 _.-]+$")
            .WithMessage("Workspace name contains invalid characters.");
    }
}

