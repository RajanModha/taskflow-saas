using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Features.Tasks;

internal static class ChecklistItemMapper
{
    public static ChecklistItemDto ToDto(ChecklistItem item) =>
        new(item.Id, item.Title, item.IsCompleted, item.Order, item.CompletedAtUtc);
}
