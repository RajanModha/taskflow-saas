using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Auth.Commands.Validators;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator(IValidator<ResetPasswordRequest> requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
