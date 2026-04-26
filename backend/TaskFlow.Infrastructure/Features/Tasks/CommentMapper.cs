using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Tasks;

internal static class CommentMapper
{
    public static CommentDto ToDto(Comment comment, ApplicationUser? author)
    {
        if (comment.IsDeleted)
        {
            return new CommentDto(
                comment.Id,
                "[deleted]",
                false,
                new DateTimeOffset(comment.CreatedAtUtc, TimeSpan.Zero),
                new DateTimeOffset(comment.UpdatedAtUtc, TimeSpan.Zero),
                null,
                true);
        }

        var authorDto = author is null
            ? null
            : new TaskAssigneeDto(author.Id, author.UserName ?? string.Empty, author.DisplayName);

        return new CommentDto(
            comment.Id,
            comment.Content,
            comment.IsEdited,
            new DateTimeOffset(comment.CreatedAtUtc, TimeSpan.Zero),
            new DateTimeOffset(comment.UpdatedAtUtc, TimeSpan.Zero),
            authorDto,
            false);
    }
}
