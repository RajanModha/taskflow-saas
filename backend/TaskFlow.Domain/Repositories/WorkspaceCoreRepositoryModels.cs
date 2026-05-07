namespace TaskFlow.Domain.Repositories;

public sealed record WorkspaceOrganizationReadModel(
    Guid Id,
    string Name,
    string JoinCode,
    DateTime CreatedAtUtc);

public sealed record WorkspaceAdminReadModel(
    Guid Id,
    Guid OrganizationId);
