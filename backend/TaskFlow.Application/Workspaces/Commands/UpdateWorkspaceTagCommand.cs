using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record UpdateWorkspaceTagCommand(Guid UserId, Guid TagId, UpdateWorkspaceTagRequest Request)
    : IRequest<(int StatusCode, object? Body)>;
