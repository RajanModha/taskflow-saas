using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Validation;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.UserName), () =>
        {
            RuleFor(x => x.UserName!)
                .Length(3, 30)
                .WithMessage("User name must be between 3 and 30 characters.")
                .Matches("^[a-zA-Z0-9_]+$")
                .WithMessage("User name may only contain letters, digits, and underscores.");
        });

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .Must(s => string.IsNullOrWhiteSpace(s) || (s.Trim().Length >= 2 && s.Trim().Length <= 50))
                .WithMessage("Display name must be between 2 and 50 characters.");
        });
    }
}
