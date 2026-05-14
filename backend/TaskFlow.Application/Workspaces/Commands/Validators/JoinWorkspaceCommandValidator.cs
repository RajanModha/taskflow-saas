using FluentValidation;

namespace TaskFlow.Application.Workspaces.Commands.Validators;

public sealed class JoinWorkspaceCommandValidator : AbstractValidator<JoinWorkspaceCommand>
{
    public JoinWorkspaceCommandValidator(IValidator<JoinWorkspaceRequest> requestValidator)
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
