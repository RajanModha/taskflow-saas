using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IUserSessionIssuer sessionIssuer,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<LoginCommand, LoginOutcome>
{
    public async Task<LoginOutcome> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        static Task DelayOnFailureAsync(CancellationToken ct) => Task.Delay(Random.Shared.Next(50, 200), ct);

        var request = command.Request;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("Invalid email or password.");
        }

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("Invalid email or password.");
        }

        if (!user.EmailVerified)
        {
            return new LoginEmailNotVerified();
        }

        if (user.OrganizationId == Guid.Empty)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, AuthRequestCommon.GetSessionConnectionInfo(httpContextAccessor), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        return new LoginSucceeded(response);
    }
}
