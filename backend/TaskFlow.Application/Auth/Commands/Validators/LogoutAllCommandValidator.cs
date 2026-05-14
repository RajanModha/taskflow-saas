using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Auth.Commands.Validators;

public sealed class LogoutAllCommandValidator : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
