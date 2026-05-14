using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class GetMyWorkspaceQueryHandler(IWorkspaceManagementService management)
    : IRequestHandler<GetMyWorkspaceQuery, MyWorkspaceResponse?>
{
    public Task<MyWorkspaceResponse?> Handle(GetMyWorkspaceQuery request, CancellationToken cancellationToken) =>
        management.GetMyWorkspaceAsync(request.UserId, cancellationToken);
}
