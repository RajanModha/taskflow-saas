using MediatR;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Tasks;

public sealed record CommentDto(
    Guid Id,
    string Content,
    bool IsEdited,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    TaskAssigneeDto? Author,
    bool IsDeleted);

public sealed record GetTaskCommentsQuery(Guid TaskId, int Page, int PageSize) : IRequest<PagedResultDto<CommentDto>?>;

public sealed record CreateTaskCommentCommand(Guid TaskId, string Content) : IRequest<CreateTaskCommentResult>;

public sealed record CreateTaskCommentResult(CommentDto? Comment, int StatusCode);

public sealed record UpdateTaskCommentCommand(Guid TaskId, Guid CommentId, string Content)
    : IRequest<UpdateTaskCommentResult>;

public sealed record UpdateTaskCommentResult(CommentDto? Body, int StatusCode, string? Detail);

public sealed record DeleteTaskCommentCommand(Guid TaskId, Guid CommentId) : IRequest<DeleteTaskCommentResult>;

public sealed record DeleteTaskCommentResult(int StatusCode);
