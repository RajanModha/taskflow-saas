namespace TaskFlow.Application.Auth;

public interface IAuthService
{
    Task<RegisterOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
