using MediatR;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Activity;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskActivityHandler(ITaskReadRepository taskRepository)
    : IRequestHandler<GetTaskActivityQuery, PagedResultDto<ActivityLogDto>?>
{
    public async Task<PagedResultDto<ActivityLogDto>?> Handle(
        GetTaskActivityQuery request,
        CancellationToken cancellationToken)
    {
        var paged = await taskRepository.GetPagedTaskActivityAsync(
            request.TaskId,
            request.Page,
            request.PageSize,
            cancellationToken);
        if (paged is null)
        {
            return null;
        }

        var items = paged.Items.Select(ActivityLogMapper.ToDto).ToList();
        return PagedResultDto<ActivityLogDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
