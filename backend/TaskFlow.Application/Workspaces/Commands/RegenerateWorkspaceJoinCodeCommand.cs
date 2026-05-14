using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record RegenerateWorkspaceJoinCodeCommand(Guid UserId)
    : IRequest<(int StatusCode, object? Body, string? Error)>;
