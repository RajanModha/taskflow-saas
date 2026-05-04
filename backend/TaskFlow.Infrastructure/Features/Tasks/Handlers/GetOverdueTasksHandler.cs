using MediatR;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetOverdueTasksHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler) : IRequestHandler<GetOverdueTasksQuery, PagedResultDto<TaskDto>>
{
    public async System.Threading.Tasks.Task<PagedResultDto<TaskDto>> Handle(
        GetOverdueTasksQuery request,
        CancellationToken cancellationToken)
    {
        var criteria = new OverdueTaskListCriteria(request.Page, request.PageSize);
        var paged = await taskRepository.GetPagedOverdueTasksAsync(criteria, cancellationToken);
        var dtos = await taskReadModelAssembler.ToTaskDtosAsync(paged.Items, cancellationToken);
        return PagedResultDto<TaskDto>.Create(dtos, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
