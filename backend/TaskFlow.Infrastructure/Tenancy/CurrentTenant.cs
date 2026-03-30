using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskFlow.Application.Tenancy;

namespace TaskFlow.Infrastructure.Tenancy;

public sealed class CurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    public bool IsSet => TryGetOrganizationId(out _);

    public Guid OrganizationId
    {
        get
        {
            if (!TryGetOrganizationId(out var id))
            {
                // Important: EF query filters may evaluate OrganizationId even when IsSet is false.
                // Returning Guid.Empty prevents runtime exceptions and keeps filters safe.
                return Guid.Empty;
            }

            return id;
        }
    }

    private bool TryGetOrganizationId(out Guid id)
    {
        id = Guid.Empty;

        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null || principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var claim = principal.FindFirst("org_id") ?? principal.FindFirst("orgId");
        return claim is not null && Guid.TryParse(claim.Value, out id) && id != Guid.Empty;
    }
}

