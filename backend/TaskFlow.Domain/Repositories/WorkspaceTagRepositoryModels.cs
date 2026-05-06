using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Repositories;

public sealed record WorkspaceActorContext(
    Guid UserId,
    Guid OrganizationId,
    WorkspaceRole WorkspaceRole);

public sealed record WorkspaceTagReadModel(
    Guid Id,
    string Name,
    string Color);
