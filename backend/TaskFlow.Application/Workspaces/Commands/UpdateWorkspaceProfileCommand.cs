using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record UpdateWorkspaceProfileCommand(Guid UserId, UpdateWorkspaceRequest Request)
    : IRequest<(int StatusCode, object? Body, string? Error)>;
