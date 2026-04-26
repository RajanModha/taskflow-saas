using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Validation;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.")
            .MaximumLength(4096)
            .WithMessage("Refresh token is too long.");
    }
}
