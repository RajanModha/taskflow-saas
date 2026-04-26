using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Validation;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters.")
            .MaximumLength(128)
            .WithMessage("New password must not exceed 128 characters.")
            .Must((req, newPass) => !string.Equals(req.CurrentPassword, newPass, StringComparison.Ordinal))
            .WithMessage("New password must be different from your current password.")
            .Matches(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$")
            .WithMessage(
                "New password must contain at least one uppercase letter, one digit, and one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords must match.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.")
            .MaximumLength(4096)
            .WithMessage("Refresh token is too long.");
    }
}
