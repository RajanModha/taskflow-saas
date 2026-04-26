using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.API.Controllers;

/// <summary>Manage projects, board views, exports, and related activity.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    public sealed record UpdateProjectRequest(string Name, string? Description);
    public sealed record ExportLimitResponse(string Detail);

    public sealed record MoveBoardTaskRequest(TaskStatus NewStatus);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProjectDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = "desc",
        CancellationToken cancellationToken = default)
    {
        var sortDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        var result = await mediator.Send(
            new GetProjectsQuery(page, pageSize, q, sortBy, sortDesc),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetProjectByIdQuery(projectId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    public sealed record CreateMilestoneRequest(string Name, string? Description, DateTime? DueDateUtc);

    public sealed record UpdateMilestoneRequest(string Name, string? Description, DateTime? DueDateUtc);

    [HttpGet("{projectId:guid}/milestones")]
    [ProducesResponseType(typeof(IReadOnlyList<MilestoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MilestoneDto>>> GetMilestones(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetMilestonesQuery(projectId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{projectId:guid}/milestones")]
    [ProducesResponseType(typeof(MilestoneDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MilestoneDto>> CreateMilestone(
        [FromRoute] Guid projectId,
        [FromBody] CreateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name is required.");
            return ValidationProblem(ModelState);
        }

        var created = await mediator.Send(
            new CreateMilestoneCommand(projectId, request.Name.Trim(), request.Description, request.DueDateUtc),
            cancellationToken);
        return created is null
            ? NotFound()
            : CreatedAtAction(nameof(GetMilestones), new { projectId }, created);
    }

    [HttpPut("{projectId:guid}/milestones/{milestoneId:guid}")]
    [ProducesResponseType(typeof(MilestoneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MilestoneDto>> UpdateMilestone(
        [FromRoute] Guid projectId,
        [FromRoute] Guid milestoneId,
        [FromBody] UpdateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name is required.");
            return ValidationProblem(ModelState);
        }

        var updated = await mediator.Send(
            new UpdateMilestoneCommand(projectId, milestoneId, request.Name.Trim(), request.Description, request.DueDateUtc),
            cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{projectId:guid}/milestones/{milestoneId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMilestone(
        [FromRoute] Guid projectId,
        [FromRoute] Guid milestoneId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await mediator.Send(new DeleteMilestoneCommand(projectId, milestoneId), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{projectId:guid}/export")]
    [EnableRateLimiting("export")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExportLimitResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Export(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetProjectExportQuery(projectId), cancellationToken);
        if (response.TaskLimitExceeded)
        {
            return BadRequest(new ExportLimitResponse("Narrow your filters. Max 10,000 rows."));
        }
        if (response.ProjectNotFound || response.Payload is null)
        {
            return NotFound();
        }

        var safeProjectName = BuildSafeFileNameSegment(response.Payload.Project.Name);
        Response.ContentType = "application/json";
        Response.Headers.ContentDisposition =
            $"attachment; filename=project-{safeProjectName}-{DateTime.UtcNow:yyyyMMdd}.json";
        await JsonSerializer.SerializeAsync(Response.Body, response.Payload, cancellationToken: cancellationToken);
        return new EmptyResult();
    }

    [HttpGet("{projectId:guid}/activity")]
    [ProducesResponseType(typeof(PagedResultDto<ActivityLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResultDto<ActivityLogDto>>> GetProjectActivity(
        [FromRoute] Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetProjectActivityQuery(projectId, page, pageSize), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{projectId:guid}/board")]
    [ProducesResponseType(typeof(ProjectBoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectBoardResponse>> GetBoard(
        [FromRoute] Guid projectId,
        [FromQuery] Guid? assigneeId = null,
        [FromQuery] Guid? tagId = null,
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetProjectBoardQuery(projectId, assigneeId, tagId, q),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{projectId:guid}/board/tasks/{taskId:guid}/move")]
    [HttpPatch("{projectId:guid}/board/tasks/{taskId:guid}/move")]
    [ProducesResponseType(typeof(BoardTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardTaskDto>> MoveBoardTask(
        [FromRoute] Guid projectId,
        [FromRoute] Guid taskId,
        [FromBody] MoveBoardTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var moved = await mediator.Send(
            new MoveProjectBoardTaskCommand(projectId, taskId, request.NewStatus),
            cancellationToken);
        return moved is null ? NotFound() : Ok(moved);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectDto>> Create(
        [FromBody] CreateProjectCommand request,
        CancellationToken cancellationToken = default)
    {
        var created = await mediator.Send(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId = created.Id }, created);
    }

    [HttpPut("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Update(
        [FromRoute] Guid projectId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var cmd = new UpdateProjectCommand(projectId, request.Name, request.Description);
        var updated = await mediator.Send(cmd, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{projectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await mediator.Send(new DeleteProjectCommand(projectId), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{projectId:guid}/restore")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Restore(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var restored = await mediator.Send(new RestoreProjectCommand(projectId), cancellationToken);
        return restored is null ? NotFound() : Ok(restored);
    }

    private static string BuildSafeFileNameSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "project" : cleaned;
    }
}

