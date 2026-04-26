using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    /// <summary>SHA-256 hex hash of the raw refresh token (UTF-8 bytes hashed).</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>SHA-256 hex hash of the replacement token after rotation.</summary>
    public string? ReplacedByTokenHash { get; set; }

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>True when the token has not been revoked and is not past its expiry.</summary>
    public bool IsActive(DateTime utcNow) =>
        !RevokedAtUtc.HasValue && ExpiresAtUtc > utcNow;
}
