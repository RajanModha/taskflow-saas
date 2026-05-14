using MediatR;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class GetWorkspaceMembersPageQueryHandler(IWorkspaceManagementService management)
    : IRequestHandler<GetWorkspaceMembersPageQuery, WorkspaceMembersPageOutcome>
{
    public async Task<WorkspaceMembersPageOutcome> Handle(
        GetWorkspaceMembersPageQuery request,
        CancellationToken cancellationToken)
    {
        WorkspaceRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!Enum.TryParse<WorkspaceRole>(request.Role, ignoreCase: true, out var parsed))
            {
                return new WorkspaceMembersPageBadRequestOutcome("Invalid role filter.");
            }

            roleFilter = parsed;
        }

        var result = await management.GetMembersPageAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            request.Q,
            roleFilter,
            cancellationToken);

        return result is null
            ? new WorkspaceMembersPageNotFoundOutcome()
            : new WorkspaceMembersPageOk(result);
    }
}
