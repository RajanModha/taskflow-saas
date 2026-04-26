namespace TaskFlow.Application.Auth;

public sealed record RegisterRequest(
    string Email,
    string UserName,
    string Password,
    string ConfirmPassword,
    string OrganizationName);

public sealed record LoginRequest(string Email, string Password);

public sealed record VerifyEmailRequest(string Token);

public sealed record ResendVerificationRequest(string Email);

public sealed record RefreshSessionRequest(string RefreshToken);

public sealed record SessionConnectionInfo(string? UserAgent, string? IpAddress);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword,
    /// <summary>Active refresh token for this device; other sessions are revoked after a successful password change.</summary>
    string RefreshToken);

public sealed record ChangePasswordResponse(string Message);

public sealed record UpdateProfileRequest(string? UserName, string? DisplayName);

/// <summary>Optional refresh token used only to mark <see cref="UserSessionItemDto.IsCurrent"/>; prefer POST over sending secrets in GET headers.</summary>
public sealed record GetSessionsRequest(string? RefreshToken = null);

public sealed record UserSessionItemDto(
    Guid Id,
    string? DeviceInfo,
    string? IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ForgotPasswordResponse(string Message);

public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);

public sealed record ResetPasswordResponse(string Message);

public sealed record RegisterPendingResponse(string Message);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string TokenType,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAt = null);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string UserName,
    IReadOnlyList<string> Roles,
    string Role,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationJoinCode,
    string? DisplayName,
    string? AvatarUrl,
    DateTimeOffset CreatedAt);
