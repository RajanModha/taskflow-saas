using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.API.Middleware;

public sealed class TenantGuardMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated == true)
        {
            var orgClaim = principal.FindFirst("org_id");
            if (orgClaim is null || !Guid.TryParse(orgClaim.Value, out var orgId) || orgId == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Title = "Tenant context missing",
                        Detail = "Authenticated requests must include org_id.",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://httpstatuses.com/400",
                    });
                return;
            }
        }

        await next(context);
    }
}

