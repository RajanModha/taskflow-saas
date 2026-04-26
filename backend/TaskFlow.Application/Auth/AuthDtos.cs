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
    DateTimeOffset? RefreshTokenExpiresAtUtc = null);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string UserName,
    IReadOnlyList<string> Roles,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationJoinCode);
