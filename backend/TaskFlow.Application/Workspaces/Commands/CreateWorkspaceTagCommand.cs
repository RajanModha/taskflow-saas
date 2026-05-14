using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record CreateWorkspaceTagCommand(Guid UserId, CreateWorkspaceTagRequest Request)
    : IRequest<(int StatusCode, object? Body)>;
