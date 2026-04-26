using FluentValidation;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Application.Validation;

public sealed class InviteMemberRequestValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => WorkspaceRoleStrings.TryParseInviteRole(r, out _))
            .WithMessage("Role must be Admin or Member.");
    }
}
