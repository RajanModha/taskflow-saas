using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskChecklistHandler(ITaskReadRepository taskRepository)
    : IRequestHandler<GetTaskChecklistQuery, IReadOnlyList<ChecklistItemDto>?>
{
    public async Task<IReadOnlyList<ChecklistItemDto>?> Handle(
        GetTaskChecklistQuery request,
        CancellationToken cancellationToken)
    {
        var items = await taskRepository.GetTaskChecklistAsync(
            request.TaskId,
            cancellationToken);
        if (items is null)
        {
            return null;
        }

        return items.Select(i =>
                new ChecklistItemDto(i.Id, i.Title, i.IsCompleted, i.Order, i.CompletedAtUtc))
            .ToList();
    }
}
