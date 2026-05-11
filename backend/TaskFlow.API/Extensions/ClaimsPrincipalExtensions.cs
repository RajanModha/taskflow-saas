using System.Security.Claims;

namespace TaskFlow.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var userIdRaw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdRaw, out var userId) ? userId : null;
    }
}
