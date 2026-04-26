using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Auth;

namespace TaskFlow.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId => ParseGuidClaim(requiredWhenAuthenticated: true, ClaimTypes.NameIdentifier, "sub");

    public string UserName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;

    public Guid OrganizationId => ParseGuidClaim(requiredWhenAuthenticated: true, "org_id", "orgId");

    public string Role => httpContextAccessor.HttpContext?.User.FindFirstValue(WorkspaceJwtClaims.Role)
        ?? string.Empty;

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    private Guid ParseGuidClaim(bool requiredWhenAuthenticated, params string[] claimTypes)
    {
        var user = httpContextAccessor.HttpContext?.User;
        foreach (var claimType in claimTypes)
        {
            var raw = user?.FindFirstValue(claimType);
            if (Guid.TryParse(raw, out var value))
            {
                return value;
            }
        }

        if (requiredWhenAuthenticated && IsAuthenticated)
        {
            throw new UnauthorizedAccessException($"Missing required claim(s): {string.Join(", ", claimTypes)}");
        }

        return Guid.Empty;
    }
}
