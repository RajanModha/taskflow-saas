using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class PermanentDeleteTaskHandler(
    ITaskWriteRepository taskRepository,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<PermanentDeleteTaskCommand, bool>
{
    public async Task<bool> Handle(PermanentDeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var deleted = await taskRepository.PermanentDeleteTaskAsync(request.TaskId, cancellationToken);
        if (deleted is null)
        {
            return false;
        }

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            deleted.OrganizationId,
            currentUser.UserId,
            deleted.AssigneeId,
            null);
        boardCacheVersion.BumpProject(deleted.ProjectId);
        return true;
    }
}
