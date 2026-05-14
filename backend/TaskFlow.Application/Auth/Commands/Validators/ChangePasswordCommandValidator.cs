using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Auth.Commands.Validators;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator(IValidator<ChangePasswordRequest> requestValidator)
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
