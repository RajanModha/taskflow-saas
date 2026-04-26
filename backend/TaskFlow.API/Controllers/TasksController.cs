using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CsvHelper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using TaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using TaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.API.Controllers;

/// <summary>Manage tasks, comments, checklists, bulk operations, and exports.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class TasksController(IMediator mediator, ITaskRepository taskRepository) : ControllerBase
{
    public sealed record CreateTaskFromTemplateOverridesRequest(
        string? Title,
        string? Description,
        TaskPriority? Priority,
        DateTime? DueDateUtc,
        Guid? AssigneeId);

    public sealed record CreateTaskFromTemplateRequest(
        Guid TemplateId,
        Guid ProjectId,
        CreateTaskFromTemplateOverridesRequest? Overrides);

    public sealed record UpdateTaskRequest(
        string Title,
        string? Description,
        TaskStatus Status,
        TaskPriority Priority,
        DateTime? DueDateUtc,
        Guid? AssigneeId,
        Guid[]? TagIds,
        Guid? MilestoneId);

    public sealed record AssignTaskRequest(Guid? AssigneeId);

    public sealed record AddChecklistItemRequest(string Title, int? InsertAfterOrder);

    public sealed record UpdateChecklistItemRequest(string? Title, bool? IsCompleted);

    public sealed record ReorderChecklistRequest(Guid[] OrderedIds);
    public sealed record BulkDeleteRequest(Guid[] TaskIds);
    public sealed record BulkAssignRequest(Guid[] TaskIds, Guid? AssigneeId);
    public sealed record TasksExportLimitResponse(string Detail);

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
        [FromQuery] Guid? milestoneId = null,
        [FromQuery] bool? isBlocked = null,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (includeDeleted && !IsAdminPlus())
        {
            return Forbid();
        }

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
                tagId,
                milestoneId,
                isBlocked,
                includeDeleted),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("export")]
    [EnableRateLimiting("export")]
    [Produces("text/csv", "application/json")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TasksExportLimitResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        [FromQuery] Guid? projectId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTime? dueFromUtc = null,
        [FromQuery] DateTime? dueToUtc = null,
        [FromQuery] string? q = null,
        [FromQuery] bool? assignedToMe = null,
        [FromQuery] Guid? assigneeId = null,
        [FromQuery] Guid? tagId = null,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
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

        var filters = new TaskExportFilters(
            projectId,
            statusEnum,
            priorityEnum,
            dueFromUtc,
            dueToUtc,
            q,
            null,
            true,
            assignedToMe,
            assigneeId,
            tagId,
            IncludeDeleted: false);

        var total = await taskRepository.GetExportCountAsync(filters, cancellationToken);
        if (total > 10_000)
        {
            return BadRequest(new TasksExportLimitResponse("Narrow your filters. Max 10,000 rows."));
        }
        var assigneeNames = await taskRepository.GetExportAssigneeDisplayNamesAsync(filters, cancellationToken);

        var exportFormat = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        switch (exportFormat)
        {
            case "csv":
                Response.ContentType = "text/csv";
                Response.Headers.ContentDisposition = $"attachment; filename=tasks-{DateTime.UtcNow:yyyyMMdd}.csv";
                await using (var writer = new StreamWriter(Response.Body))
                await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteHeader<TaskExportRow>();
                    await csv.NextRecordAsync();
                    await foreach (var task in taskRepository.GetExportStreamAsync(filters, cancellationToken))
                    {
                        csv.WriteRecord(MapToExportRow(task, assigneeNames));
                        await csv.NextRecordAsync();
                    }
                }

                return new EmptyResult();

            case "json":
                Response.ContentType = "application/json";
                Response.Headers.ContentDisposition = $"attachment; filename=tasks-{DateTime.UtcNow:yyyyMMdd}.json";
                await JsonSerializer.SerializeAsync(
                    Response.Body,
                    StreamExportRows(filters, assigneeNames, cancellationToken),
                    cancellationToken: cancellationToken);
                return new EmptyResult();

            default:
                ModelState.AddModelError(nameof(format), "Invalid format. Use csv or json.");
                return ValidationProblem(ModelState);
        }
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

    public sealed record AddTaskDependencyRequest(Guid BlockingTaskId);

    public sealed record CycleErrorResponse(string Detail);

    [HttpGet("{taskId:guid}/dependencies")]
    [ProducesResponseType(typeof(TaskDependenciesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDependenciesResponse>> GetDependencies(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetTaskDependenciesQuery(taskId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{taskId:guid}/dependencies")]
    [ProducesResponseType(typeof(DependencyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CycleErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddDependency(
        [FromRoute] Guid taskId,
        [FromBody] AddTaskDependencyRequest request,
        CancellationToken cancellationToken = default)
    {
        var outcome = await mediator.Send(
            new AddTaskDependencyCommand(taskId, request.BlockingTaskId),
            cancellationToken);
        return outcome switch
        {
            AddTaskDependencyResult.Ok ok => CreatedAtAction(nameof(GetDependencies), new { taskId }, ok.Dependency),
            AddTaskDependencyResult.Cycle => BadRequest(new CycleErrorResponse("This dependency would create a circular chain.")),
            AddTaskDependencyResult.SelfReference => BadRequest(new CycleErrorResponse("A task cannot depend on itself.")),
            AddTaskDependencyResult.MaxDependencies => BadRequest(new { detail = "This task already has the maximum of 10 dependencies." }),
            AddTaskDependencyResult.Duplicate => Conflict(),
            AddTaskDependencyResult.NotFound => NotFound(),
            _ => NotFound(),
        };
    }

    [HttpDelete("{taskId:guid}/dependencies/{blockingTaskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDependency(
        [FromRoute] Guid taskId,
        [FromRoute] Guid blockingTaskId,
        CancellationToken cancellationToken = default)
    {
        var removed = await mediator.Send(
            new RemoveTaskDependencyCommand(taskId, blockingTaskId),
            cancellationToken);
        return removed ? NoContent() : NotFound();
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
    [RequestSizeLimit(51200)]
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
                "Project was not found in your workspace, the assignee is invalid, the milestone is invalid, or one or more tags are not in this workspace.");
            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(GetById), new { taskId = created.Id }, created);
    }

    [HttpPost("from-template")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> CreateFromTemplate(
        [FromBody] CreateTaskFromTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTaskFromTemplateCommand(
            request.TemplateId,
            request.ProjectId,
            request.Overrides is null
                ? null
                : new CreateTaskFromTemplateOverrides(
                    request.Overrides.Title,
                    request.Overrides.Description,
                    request.Overrides.Priority,
                    request.Overrides.DueDateUtc,
                    request.Overrides.AssigneeId));

        var created = await mediator.Send(command, cancellationToken);
        if (created is null)
        {
            ModelState.AddModelError(
                "templateId",
                "Template or project was not found in your workspace, or the assignee is invalid.");
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
            request.TagIds,
            request.MilestoneId);
        var updated = await mediator.Send(cmd, cancellationToken);
        if (updated is null)
        {
            ModelState.AddModelError(
                "assigneeId",
                "Task was not found, the assignee is invalid, the milestone is invalid, or one or more tags are not in this workspace.");
            return ValidationProblem(ModelState);
        }

        return Ok(updated);
    }

    [HttpPatch("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Patch(
        [FromRoute] Guid taskId,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken = default)
    {
        var hasTitle = body.TryGetProperty("title", out var titleProp);
        var hasDescription = body.TryGetProperty("description", out var descriptionProp);
        var hasStatus = body.TryGetProperty("status", out var statusProp);
        var hasPriority = body.TryGetProperty("priority", out var priorityProp);
        var hasDueDateUtc = body.TryGetProperty("dueDateUtc", out var dueDateProp);
        var hasAssigneeId = body.TryGetProperty("assigneeId", out var assigneeProp);

        if (!(hasTitle || hasDescription || hasStatus || hasPriority || hasDueDateUtc || hasAssigneeId))
        {
            ModelState.AddModelError("body", "At least one patch field is required.");
            return ValidationProblem(ModelState);
        }

        string? title = hasTitle && titleProp.ValueKind != JsonValueKind.Null ? titleProp.GetString() : null;
        string? description = hasDescription && descriptionProp.ValueKind != JsonValueKind.Null ? descriptionProp.GetString() : null;
        DateTime? dueDateUtc = hasDueDateUtc && dueDateProp.ValueKind != JsonValueKind.Null ? dueDateProp.GetDateTime() : null;
        Guid? assigneeId = hasAssigneeId && assigneeProp.ValueKind != JsonValueKind.Null ? assigneeProp.GetGuid() : null;

        TaskStatus? status = null;
        if (hasStatus && statusProp.ValueKind != JsonValueKind.Null)
        {
            if (statusProp.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse<TaskStatus>(statusProp.GetString(), true, out var parsed))
                {
                    ModelState.AddModelError("status", "Invalid status.");
                    return ValidationProblem(ModelState);
                }
                status = parsed;
            }
            else
            {
                status = (TaskStatus)statusProp.GetInt32();
            }
        }

        TaskPriority? priority = null;
        if (hasPriority && priorityProp.ValueKind != JsonValueKind.Null)
        {
            if (priorityProp.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse<TaskPriority>(priorityProp.GetString(), true, out var parsed))
                {
                    ModelState.AddModelError("priority", "Invalid priority.");
                    return ValidationProblem(ModelState);
                }
                priority = parsed;
            }
            else
            {
                priority = (TaskPriority)priorityProp.GetInt32();
            }
        }

        var command = new PatchTaskCommand(
            taskId,
            title,
            hasTitle,
            description,
            hasDescription,
            status,
            hasStatus,
            priority,
            hasPriority,
            dueDateUtc,
            hasDueDateUtc,
            assigneeId,
            hasAssigneeId);
        var result = await mediator.Send(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
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

    [HttpPost("bulk-update")]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(BulkTaskOperationResultDto), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkTaskOperationResultDto>> BulkUpdate(
        [FromBody] JsonElement body,
        CancellationToken cancellationToken = default)
    {
        if (!body.TryGetProperty("taskIds", out var taskIdsProp) || taskIdsProp.ValueKind != JsonValueKind.Array)
        {
            ModelState.AddModelError("taskIds", "taskIds is required.");
            return ValidationProblem(ModelState);
        }
        var taskIds = new List<Guid>();
        foreach (var element in taskIdsProp.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String || !Guid.TryParse(element.GetString(), out var id))
            {
                ModelState.AddModelError("taskIds", "All taskIds must be valid GUID strings.");
                return ValidationProblem(ModelState);
            }

            taskIds.Add(id);
        }
        if (taskIds.Count > 100)
        {
            ModelState.AddModelError("taskIds", "No more than 100 task ids are allowed.");
            return ValidationProblem(ModelState);
        }

        if (!body.TryGetProperty("updates", out var updatesProp) || updatesProp.ValueKind != JsonValueKind.Object)
        {
            ModelState.AddModelError("updates", "updates is required.");
            return ValidationProblem(ModelState);
        }
        var hasDueDateUtc = updatesProp.TryGetProperty("dueDateUtc", out var dueDateProp);
        var hasAssigneeId = updatesProp.TryGetProperty("assigneeId", out var assigneeProp);
        var hasStatus = updatesProp.TryGetProperty("status", out var statusProp);
        var hasPriority = updatesProp.TryGetProperty("priority", out var priorityProp);

        TaskStatus? status = null;
        if (hasStatus && statusProp.ValueKind != JsonValueKind.Null)
        {
            if (statusProp.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse<TaskStatus>(statusProp.GetString(), true, out var parsed))
                {
                    ModelState.AddModelError("updates.status", "Invalid status.");
                    return ValidationProblem(ModelState);
                }
                status = parsed;
            }
            else if (statusProp.ValueKind == JsonValueKind.Number && statusProp.TryGetInt32(out var numericStatus))
            {
                status = (TaskStatus)numericStatus;
            }
            else
            {
                ModelState.AddModelError("updates.status", "Invalid status.");
                return ValidationProblem(ModelState);
            }
        }

        TaskPriority? priority = null;
        if (hasPriority && priorityProp.ValueKind != JsonValueKind.Null)
        {
            if (priorityProp.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse<TaskPriority>(priorityProp.GetString(), true, out var parsed))
                {
                    ModelState.AddModelError("updates.priority", "Invalid priority.");
                    return ValidationProblem(ModelState);
                }
                priority = parsed;
            }
            else if (priorityProp.ValueKind == JsonValueKind.Number && priorityProp.TryGetInt32(out var numericPriority))
            {
                priority = (TaskPriority)numericPriority;
            }
            else
            {
                ModelState.AddModelError("updates.priority", "Invalid priority.");
                return ValidationProblem(ModelState);
            }
        }

        DateTime? dueDateUtc = null;
        if (hasDueDateUtc && dueDateProp.ValueKind != JsonValueKind.Null)
        {
            if (dueDateProp.ValueKind != JsonValueKind.String || !dueDateProp.TryGetDateTime(out var parsedDueDate))
            {
                ModelState.AddModelError("updates.dueDateUtc", "dueDateUtc must be a valid ISO-8601 date/time string or null.");
                return ValidationProblem(ModelState);
            }

            dueDateUtc = parsedDueDate;
        }

        Guid? assigneeId = null;
        if (hasAssigneeId && assigneeProp.ValueKind != JsonValueKind.Null)
        {
            if (assigneeProp.ValueKind != JsonValueKind.String || !Guid.TryParse(assigneeProp.GetString(), out var parsedAssigneeId))
            {
                ModelState.AddModelError("updates.assigneeId", "assigneeId must be a valid GUID string or null.");
                return ValidationProblem(ModelState);
            }

            assigneeId = parsedAssigneeId;
        }

        var command = new BulkUpdateTasksCommand(
            [.. taskIds],
            new BulkTaskUpdateFields(
                status,
                priority,
                dueDateUtc,
                assigneeId,
                hasDueDateUtc,
                hasAssigneeId));
        var result = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status207MultiStatus, result);
    }

    [HttpPost("bulk-delete")]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(BulkTaskDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkTaskDeleteResultDto>> BulkDelete(
        [FromBody] BulkDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TaskIds.Length > 100)
        {
            ModelState.AddModelError(nameof(request.TaskIds), "No more than 100 task ids are allowed.");
            return ValidationProblem(ModelState);
        }

        var result = await mediator.Send(new BulkDeleteTasksCommand(request.TaskIds), cancellationToken);
        return Ok(result);
    }

    [HttpPost("bulk-assign")]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(BulkTaskOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkTaskOperationResultDto>> BulkAssign(
        [FromBody] BulkAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TaskIds.Length > 100)
        {
            ModelState.AddModelError(nameof(request.TaskIds), "No more than 100 task ids are allowed.");
            return ValidationProblem(ModelState);
        }

        var result = await mediator.Send(new BulkAssignTasksCommand(request.TaskIds, request.AssigneeId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{taskId:guid}/restore")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Restore(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var restored = await mediator.Send(new RestoreTaskCommand(taskId), cancellationToken);
        return restored is null ? NotFound() : Ok(restored);
    }

    [HttpDelete("{taskId:guid}/permanent")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PermanentDelete(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await mediator.Send(new PermanentDeleteTaskCommand(taskId), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private async IAsyncEnumerable<TaskExportRow> StreamExportRows(
        TaskExportFilters filters,
        IReadOnlyDictionary<Guid, string> assigneeNames,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var task in taskRepository.GetExportStreamAsync(filters, cancellationToken))
        {
            yield return MapToExportRow(task, assigneeNames);
        }
    }

    private static TaskExportRow MapToExportRow(DomainTask task, IReadOnlyDictionary<Guid, string> assigneeNames)
    {
        string? assigneeName = null;
        if (task.AssigneeId is { } assigneeId && assigneeNames.TryGetValue(assigneeId, out var name))
        {
            assigneeName = name;
        }
        var tags = task.TaskTags
            .Select(tt => tt.Tag.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        return new TaskExportRow
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            ProjectName = task.Project?.Name ?? string.Empty,
            AssigneeName = assigneeName,
            Tags = string.Join(", ", tags),
            DueDateUtc = task.DueDateUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
        };
    }

    private bool IsAdminPlus()
    {
        var role = User.FindFirst(WorkspaceJwtClaims.Role)?.Value;
        return role is WorkspaceRoleStrings.Owner or WorkspaceRoleStrings.Admin;
    }

}
