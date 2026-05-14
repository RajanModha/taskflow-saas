using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Auth.Commands.Validators;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator(IValidator<ForgotPasswordRequest> requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
