using MediatR;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskCommentsHandler(ITaskReadRepository taskRepository)
    : IRequestHandler<GetTaskCommentsQuery, PagedResultDto<CommentDto>?>
{
    public async System.Threading.Tasks.Task<PagedResultDto<CommentDto>?> Handle(
        GetTaskCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var paged = await taskRepository.GetPagedTaskCommentsAsync(
            request.TaskId,
            request.Page,
            request.PageSize,
            cancellationToken);
        if (paged is null)
        {
            return null;
        }

        var items = paged.Items.Select(c =>
            {
                if (c.IsDeleted)
                {
                    return new CommentDto(
                        c.Id,
                        "[deleted]",
                        false,
                        new DateTimeOffset(c.CreatedAtUtc, TimeSpan.Zero),
                        new DateTimeOffset(c.UpdatedAtUtc, TimeSpan.Zero),
                        null,
                        true);
                }

                var author = c.AuthorId is { } authorId
                    ? new TaskAssigneeDto(authorId, c.AuthorUserName ?? string.Empty, c.AuthorDisplayName)
                    : null;

                return new CommentDto(
                    c.Id,
                    c.Content,
                    c.IsEdited,
                    new DateTimeOffset(c.CreatedAtUtc, TimeSpan.Zero),
                    new DateTimeOffset(c.UpdatedAtUtc, TimeSpan.Zero),
                    author,
                    false);
            })
            .ToList();

        return PagedResultDto<CommentDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
