using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using TaskFlow.Application.Dashboard;
using TaskFlow.API.Swagger;
using Asp.Versioning;

namespace TaskFlow.API.Controllers;

/// <summary>Provide dashboard analytics for the current workspace and user.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DashboardStatsExampleProvider))]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-stats")]
    [ProducesResponseType(typeof(DashboardMyStatsDto), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DashboardMyStatsExampleProvider))]
    public async Task<ActionResult<DashboardMyStatsDto>> GetMyStats(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetDashboardMyStatsQuery(), cancellationToken);
        return Ok(result);
    }
}
