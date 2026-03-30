namespace TaskFlow.Domain.Entities;

/// <summary>
/// Domain representation of an application user (passwords and claims live in the auth store).
/// </summary>
public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public Guid OrganizationId { get; init; }
}
