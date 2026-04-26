namespace TaskFlow.Application.Auth;

public interface IAuthService
{
    Task<RegisterOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<VerifyEmailOutcome> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);

    Task ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken cancellationToken = default);

    Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<RefreshSessionOutcome> RefreshSessionAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(Guid userId, LogoutRequest request, CancellationToken cancellationToken = default);

    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSessionItemDto>> GetSessionsAsync(
        Guid userId,
        string? refreshTokenRawForCurrentMarker,
        CancellationToken cancellationToken = default);

    /// <summary>Returns false if the session id does not belong to the user.</summary>
    Task<bool> TryRevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<ResetPasswordOutcome> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateProfileOutcome> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
