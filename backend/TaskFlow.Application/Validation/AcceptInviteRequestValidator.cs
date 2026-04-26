using FluentValidation;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Application.Validation;

public sealed class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
