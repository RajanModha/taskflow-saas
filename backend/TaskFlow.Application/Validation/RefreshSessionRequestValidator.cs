using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Validation;

public sealed class RefreshSessionRequestValidator : AbstractValidator<RefreshSessionRequest>
{
    public RefreshSessionRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(2048);
    }
}
