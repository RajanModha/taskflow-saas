using FluentValidation;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Application.Validation;

public sealed class ResendInviteRequestValidator : AbstractValidator<ResendInviteRequest>
{
    public ResendInviteRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
