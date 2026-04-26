using FluentValidation;
using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Validation;

public sealed class GetSessionsRequestValidator : AbstractValidator<GetSessionsRequest>
{
    public GetSessionsRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.RefreshToken), () =>
        {
            RuleFor(x => x.RefreshToken!)
                .MaximumLength(4096)
                .WithMessage("Refresh token is too long.");
        });
    }
}
