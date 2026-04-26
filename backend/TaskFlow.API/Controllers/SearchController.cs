using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Application.Search;

namespace TaskFlow.API.Controllers;

/// <summary>Run workspace-wide search across supported entities.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class SearchController(IMediator mediator) : ControllerBase
{
    /// <summary>Workspace-wide read-only search across tasks, projects, and comments.</summary>
    [HttpGet]
    [EnableRateLimiting("api")]
    [ProducesResponseType(typeof(SearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResultDto>> Get(
        [FromQuery] string q,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            ModelState.AddModelError(nameof(q), "Query is required.");
            return ValidationProblem(ModelState);
        }

        var query = q.Trim();
        if (query.Length < 2)
        {
            ModelState.AddModelError(nameof(q), "Query must be at least 2 characters.");
            return ValidationProblem(ModelState);
        }

        var boundedLimit = Math.Clamp(limit, 1, 20);
        var result = await mediator.Send(new GetWorkspaceSearchQuery(query, boundedLimit), cancellationToken);
        return Ok(result);
    }
}
