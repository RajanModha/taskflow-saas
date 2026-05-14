using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class UpdateWorkspaceProfileCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<UpdateWorkspaceProfileCommand, (int StatusCode, object? Body, string? Error)>
{
    public Task<(int StatusCode, object? Body, string? Error)> Handle(
        UpdateWorkspaceProfileCommand request,
        CancellationToken cancellationToken) =>
        management.UpdateWorkspaceNameAsync(request.UserId, request.Request, cancellationToken);
}
