using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InfoController(IAppInfo appInfo) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(InfoResponse), StatusCodes.Status200OK)]
    public ActionResult<InfoResponse> Get()
    {
        return Ok(new InfoResponse(appInfo.ApplicationName));
    }
}

public sealed record InfoResponse(string ApplicationName);
