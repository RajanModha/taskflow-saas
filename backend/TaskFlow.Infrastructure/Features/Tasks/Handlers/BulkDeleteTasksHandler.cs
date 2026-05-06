using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class BulkDeleteTasksHandler(
    ITaskBulkRepository taskRepository,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<BulkDeleteTasksCommand, BulkTaskDeleteResultDto>
{
    public async Task<BulkTaskDeleteResultDto> Handle(BulkDeleteTasksCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.BulkSoftDeleteTasksAsync(request.TaskIds, cancellationToken);
        foreach (var task in result.Mutated)
        {
            DashboardCacheInvalidation.InvalidateAfterTaskMutation(
                cache,
                task.OrganizationId,
                currentUser.UserId,
                task.PreviousAssigneeId,
                null);
            boardCacheVersion.BumpProject(task.ProjectId);
        }

        return new BulkTaskDeleteResultDto(result.Mutated.Count, result.NotFound);
    }
}
