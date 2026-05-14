using FluentValidation;

namespace TaskFlow.Application.Workspaces.Commands.Validators;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator(IValidator<CreateWorkspaceRequest> requestValidator)
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
