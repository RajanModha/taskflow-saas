using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Validation;

/// <summary>Structural validation only; password policy is enforced by ASP.NET Core Identity in AuthService.</summary>
public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required.")
            .MaximumLength(2048)
            .WithMessage("Reset token is too long.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MaximumLength(128)
            .WithMessage("New password must not exceed 128 characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Please confirm your new password.")
            .Equal(x => x.NewPassword)
            .WithMessage("Confirm password must match the new password.");
    }
}
