namespace TaskFlow.Application.Auth;

public sealed record RegisterRequest(
    string Email,
    string UserName,
    string Password,
    string ConfirmPassword,
    string OrganizationName);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, string TokenType);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string UserName,
    IReadOnlyList<string> Roles,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationJoinCode);
