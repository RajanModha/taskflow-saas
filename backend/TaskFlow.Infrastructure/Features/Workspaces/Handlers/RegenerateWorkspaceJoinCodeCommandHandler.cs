using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class RegenerateWorkspaceJoinCodeCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<RegenerateWorkspaceJoinCodeCommand, (int StatusCode, object? Body, string? Error)>
{
    public Task<(int StatusCode, object? Body, string? Error)> Handle(
        RegenerateWorkspaceJoinCodeCommand request,
        CancellationToken cancellationToken) =>
        management.RegenerateJoinCodeAsync(request.UserId, cancellationToken);
}
