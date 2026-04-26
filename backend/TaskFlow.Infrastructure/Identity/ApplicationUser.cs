using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; }
    public Guid OrganizationId { get; set; }

    public bool EmailVerified { get; set; }

    /// <summary>SHA-256 hex hash of the raw verification token (UTF-8 bytes hashed).</summary>
    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiry { get; set; }

    public DateTime? LastVerificationResendAt { get; set; }

    public int VerificationResendCount { get; set; }

    /// <summary>SHA-256 hex hash of the raw password-reset token (UTF-8 bytes hashed).</summary>
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    public bool PasswordResetUsed { get; set; }

    /// <summary>Last time a forgot-password request was processed for this user (rate-limit tracking).</summary>
    public DateTime? LastResetRequestAt { get; set; }

    public int PasswordResetRequestsThisHour { get; set; }

    public DateTime? PasswordResetHourStartedUtc { get; set; }
}
