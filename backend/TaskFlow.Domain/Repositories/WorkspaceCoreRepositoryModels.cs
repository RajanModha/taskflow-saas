namespace TaskFlow.Domain.Repositories;

public sealed record WorkspaceOrganizationReadModel(
    Guid Id,
    string Name,
    string JoinCode);

public sealed record WorkspaceAdminReadModel(
    Guid Id,
    Guid OrganizationId);
