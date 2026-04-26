namespace TaskFlow.Domain.Entities;

public sealed class PendingInvite
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public WorkspaceRole Role { get; set; }

    /// <summary>SHA-256 hex hash of the raw invite token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime SentAtUtc { get; set; }

    public int ResendCount { get; set; }

    public DateTime? LastResentAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }
}
