using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using TaskPriority = TaskFlow.Domain.Entities.TaskPriority;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TasksController(IMediator mediator) : ControllerBase
{
    public sealed record UpdateTaskRequest(
        string Title,
        string? Description,
        TaskStatus Status,
        TaskPriority Priority,
        DateTime? DueDateUtc);

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
            new GetTasksQuery(page, pageSize, projectId, statusEnum, priorityEnum, dueFromUtc, dueToUtc, q, sortBy, sortDesc),
            cancellationToken);

        return Ok(result);
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
            ModelState.AddModelError("projectId", "Project not found (or not in your workspace).");
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
            request.DueDateUtc);
        var updated = await mediator.Send(cmd, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
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

