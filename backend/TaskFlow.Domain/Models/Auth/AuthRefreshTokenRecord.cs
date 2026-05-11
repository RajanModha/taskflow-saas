namespace TaskFlow.Domain.Models.Auth;

public sealed record AuthRefreshTokenRecord(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc);
