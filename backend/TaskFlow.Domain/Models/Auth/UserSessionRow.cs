namespace TaskFlow.Domain.Models.Auth;

public sealed record UserSessionRow(
    Guid Id,
    string? DeviceInfo,
    string? IpAddress,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string TokenHash);
