using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Repositories;

public sealed record WorkspaceUserReadModel(
    Guid Id,
    Guid OrganizationId,
    WorkspaceRole WorkspaceRole,
    DateTime WorkspaceJoinedAtUtc,
    string? UserName,
    string? DisplayName,
    string? Email,
    string? NormalizedEmail);

public sealed record WorkspaceMemberPageRowReadModel(
    Guid Id,
    string UserName,
    string? DisplayName,
    string Email,
    WorkspaceRole WorkspaceRole,
    DateTime WorkspaceJoinedAtUtc);

public sealed record WorkspacePendingInviteReadModel(
    Guid Id,
    Guid OrganizationId,
    string Email,
    string NormalizedEmail,
    WorkspaceRole Role,
    string TokenHash,
    DateTime ExpiresAtUtc,
    DateTime SentAtUtc,
    int ResendCount,
    DateTime? LastResentAtUtc,
    DateTime? AcceptedAtUtc);

public sealed record WorkspacePendingInviteMutationInput(
    Guid OrganizationId,
    string Email,
    string NormalizedEmail,
    WorkspaceRole Role,
    string TokenHash,
    DateTime ExpiresAtUtc,
    DateTime SentAtUtc,
    int ResendCount,
    DateTime? LastResentAtUtc,
    DateTime? AcceptedAtUtc);
