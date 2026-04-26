using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Auth;

/// <summary>Issues a new access token and refresh token pair and persists the refresh hash on the user.</summary>
public interface IUserSessionIssuer
{
    Task<AuthResponse> IssueSessionAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
