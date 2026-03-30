using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    public sealed record UpdateProjectRequest(string Name, string? Description);

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
}

