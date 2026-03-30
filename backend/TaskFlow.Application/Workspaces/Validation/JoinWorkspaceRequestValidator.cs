using FluentValidation;

namespace TaskFlow.Application.Workspaces.Validation;

public sealed class JoinWorkspaceRequestValidator : AbstractValidator<JoinWorkspaceRequest>
{
    public JoinWorkspaceRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(8)
            .Matches("^[A-Z0-9]{8}$")
            .WithMessage("Join code must be 8 characters (A-Z, 0-9).");
    }
}

