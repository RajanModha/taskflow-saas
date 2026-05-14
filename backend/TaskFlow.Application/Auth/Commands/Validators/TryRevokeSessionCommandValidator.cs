using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Auth.Commands.Validators;

public sealed class TryRevokeSessionCommandValidator : AbstractValidator<TryRevokeSessionCommand>
{
    public TryRevokeSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
