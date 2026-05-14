using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Auth.Validation;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(2048);
    }
}
