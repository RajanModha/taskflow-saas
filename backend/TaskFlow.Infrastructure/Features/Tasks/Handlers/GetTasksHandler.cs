using MediatR;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTasksHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser) : IRequestHandler<GetTasksQuery, PagedResultDto<TaskDto>>
{
    public async System.Threading.Tasks.Task<PagedResultDto<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var assigneeId = request.AssigneeId;
        var forceEmptyResult = false;
        if (request.AssignedToMe == true)
        {
            if (currentUser.UserId is { } me)
            {
                assigneeId = me;
            }
            else
            {
                forceEmptyResult = true;
            }
        }

        var criteria = new TaskListCriteria(
            request.Page,
            request.PageSize,
            request.ProjectId,
            request.Status,
            request.Priority,
            request.DueFromUtc,
            request.DueToUtc,
            request.Q,
            request.SortBy,
            request.SortDesc,
            assigneeId,
            request.TagId,
            request.MilestoneId,
            request.IsBlocked,
            request.IncludeDeleted,
            request.DeletedOnly,
            forceEmptyResult);

        var paged = await taskRepository.GetPagedTasksAsync(criteria, cancellationToken);
        var dtos = await taskReadModelAssembler.ToTaskDtosAsync(paged.Items, cancellationToken);
        return PagedResultDto<TaskDto>.Create(dtos, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
