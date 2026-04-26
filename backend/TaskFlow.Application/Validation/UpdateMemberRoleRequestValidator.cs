using FluentValidation;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Application.Validation;

public sealed class UpdateMemberRoleRequestValidator : AbstractValidator<UpdateMemberRoleRequest>
{
    public UpdateMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => WorkspaceRoleStrings.TryParseInviteRole(r, out _))
            .WithMessage("Role must be Admin or Member.");
    }
}
