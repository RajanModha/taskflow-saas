using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using TaskPriority = TaskFlow.Domain.Entities.TaskPriority;

namespace TaskFlow.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/[controller]")]
public sealed class TasksController(IMediator mediator) : ControllerBase
{
    public sealed record UpdateTaskRequest(
        string Title,
        string? Description,
        TaskStatus Status,
        TaskPriority Priority,
        DateTime? DueDateUtc,
        Guid? AssigneeId,
        Guid[]? TagIds);

    public sealed record AssignTaskRequest(Guid? AssigneeId);

    public sealed record AddChecklistItemRequest(string Title, int? InsertAfterOrder);

    public sealed record UpdateChecklistItemRequest(string? Title, bool? IsCompleted);

    public sealed record ReorderChecklistRequest(Guid[] OrderedIds);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<TaskDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? projectId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTime? dueFromUtc = null,
        [FromQuery] DateTime? dueToUtc = null,
        [FromQuery] string? q = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = "desc",
        [FromQuery] bool? assignedToMe = null,
        [FromQuery] Guid? assigneeId = null,
        [FromQuery] Guid? tagId = null,
        CancellationToken cancellationToken = default)
    {
        var sortDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        TaskStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<TaskStatus>(status, ignoreCase: true, out var parsed))
            {
                ModelState.AddModelError(nameof(status), "Invalid status.");
                return ValidationProblem(ModelState);
            }

            statusEnum = parsed;
        }

        TaskPriority? priorityEnum = null;
        if (!string.IsNullOrWhiteSpace(priority))
        {
            if (!Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsed))
            {
                ModelState.AddModelError(nameof(priority), "Invalid priority.");
                return ValidationProblem(ModelState);
            }

            priorityEnum = parsed;
        }

        var result = await mediator.Send(
            new GetTasksQuery(
                page,
                pageSize,
                projectId,
                statusEnum,
                priorityEnum,
                dueFromUtc,
                dueToUtc,
                q,
                sortBy,
                sortDesc,
                assignedToMe,
                assigneeId,
                tagId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("overdue")]
    [ProducesResponseType(typeof(PagedResultDto<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<TaskDto>>> GetOverdue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetOverdueTasksQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    public sealed record CreateCommentRequest(string Content);

    public sealed record UpdateCommentRequest(string Content);

    [HttpGet("{taskId:guid}/comments")]
    [ProducesResponseType(typeof(PagedResultDto<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResultDto<CommentDto>>> GetComments(
        [FromRoute] Guid taskId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetTaskCommentsQuery(taskId, page, pageSize), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{taskId:guid}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> CreateComment(
        [FromRoute] Guid taskId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var outcome = await mediator.Send(new CreateTaskCommentCommand(taskId, request.Content), cancellationToken);
        return outcome.StatusCode switch
        {
            StatusCodes.Status201Created => CreatedAtAction(
                nameof(GetComments),
                new { taskId },
                outcome.Comment),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status400BadRequest => BadRequest(new { message = "Content is too long after encoding." }),
            _ => NotFound(),
        };
    }

    [HttpPut("{taskId:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> UpdateComment(
        [FromRoute] Guid taskId,
        [FromRoute] Guid commentId,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var outcome = await mediator.Send(
            new UpdateTaskCommentCommand(taskId, commentId, request.Content),
            cancellationToken);

        return outcome.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(outcome.Body),
            StatusCodes.Status403Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { detail = outcome.Detail }),
            StatusCodes.Status400BadRequest => BadRequest(new { message = outcome.Detail }),
            _ => NotFound(),
        };
    }

    [HttpDelete("{taskId:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] Guid taskId,
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var outcome = await mediator.Send(new DeleteTaskCommentCommand(taskId, commentId), cancellationToken);
        return outcome.StatusCode switch
        {
            StatusCodes.Status204NoContent => NoContent(),
            StatusCodes.Status403Forbidden => Forbid(),
            _ => NotFound(),
        };
    }

    [HttpPost("{taskId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> AddTaskTag(
        [FromRoute] Guid taskId,
        [FromRoute] Guid tagId,
        CancellationToken cancellationToken = default)
    {
        var updated = await mediator.Send(new AddTaskTagCommand(taskId, tagId), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{taskId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTaskTag(
        [FromRoute] Guid taskId,
        [FromRoute] Guid tagId,
        CancellationToken cancellationToken = default)
    {
        var status = await mediator.Send(new RemoveTaskTagCommand(taskId, tagId), cancellationToken);
        return status == StatusCodes.Status204NoContent ? NoContent() : NotFound();
    }

    [HttpGet("{taskId:guid}/checklist")]
    [ProducesResponseType(typeof(IReadOnlyList<ChecklistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemDto>>> GetChecklist(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var rows = await mediator.Send(new GetTaskChecklistQuery(taskId), cancellationToken);
        return rows is null ? NotFound() : Ok(rows);
    }

    [HttpPost("{taskId:guid}/checklist/reorder")]
    [ProducesResponseType(typeof(IReadOnlyList<ChecklistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemDto>>> ReorderChecklist(
        [FromRoute] Guid taskId,
        [FromBody] ReorderChecklistRequest request,
        CancellationToken cancellationToken = default)
    {
        var ordered = await mediator.Send(new ReorderChecklistCommand(taskId, request.OrderedIds), cancellationToken);
        return ordered is null ? NotFound() : Ok(ordered);
    }

    [HttpPost("{taskId:guid}/checklist")]
    [ProducesResponseType(typeof(ChecklistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistItemDto>> AddChecklistItem(
        [FromRoute] Guid taskId,
        [FromBody] AddChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await mediator.Send(
            new AddChecklistItemCommand(taskId, request.Title, request.InsertAfterOrder),
            cancellationToken);
        return created is null
            ? NotFound()
            : CreatedAtAction(nameof(GetChecklist), new { taskId }, created);
    }

    [HttpPut("{taskId:guid}/checklist/{itemId:guid}")]
    [ProducesResponseType(typeof(ChecklistItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChecklistItemDto>> UpdateChecklistItem(
        [FromRoute] Guid taskId,
        [FromRoute] Guid itemId,
        [FromBody] UpdateChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await mediator.Send(
            new UpdateChecklistItemCommand(taskId, itemId, request.Title, request.IsCompleted),
            cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{taskId:guid}/checklist/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChecklistItem(
        [FromRoute] Guid taskId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var ok = await mediator.Send(new DeleteChecklistItemCommand(taskId, itemId), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("{taskId:guid}/activity")]
    [ProducesResponseType(typeof(PagedResultDto<ActivityLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResultDto<ActivityLogDto>>> GetTaskActivity(
        [FromRoute] Guid taskId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetTaskActivityQuery(taskId, page, pageSize), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetTaskByIdQuery(taskId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskDto>> Create(
        [FromBody] CreateTaskCommand request,
        CancellationToken cancellationToken = default)
    {
        var created = await mediator.Send(request, cancellationToken);
        if (created is null)
        {
            ModelState.AddModelError(
                "projectId",
                "Project was not found in your workspace, the assignee is invalid, or one or more tags are not in this workspace.");
            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(GetById), new { taskId = created.Id }, created);
    }

    [HttpPut("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Update(
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var cmd = new UpdateTaskCommand(
            taskId,
            request.Title,
            request.Description,
            request.Status,
            request.Priority,
            request.DueDateUtc,
            request.AssigneeId,
            request.TagIds);
        var updated = await mediator.Send(cmd, cancellationToken);
        if (updated is null)
        {
            ModelState.AddModelError(
                "assigneeId",
                "Task was not found, the assignee is invalid, or one or more tags are not in this workspace.");
            return ValidationProblem(ModelState);
        }

        return Ok(updated);
    }

    [HttpPut("{taskId:guid}/assign")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Assign(
        [FromRoute] Guid taskId,
        [FromBody] AssignTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await mediator.Send(new AssignTaskCommand(taskId, request.AssigneeId), cancellationToken);
        if (updated is null)
        {
            ModelState.AddModelError(
                "assigneeId",
                "Task was not found, or the assignee is not a member of this workspace.");
            return ValidationProblem(ModelState);
        }

        return Ok(updated);
    }

    [HttpDelete("{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await mediator.Send(new DeleteTaskCommand(taskId), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
