using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Auth;

public interface IJwtTokenGenerator
{
    string CreateAccessToken(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        Guid organizationId,
        WorkspaceRole workspaceRole,
        DateTime utcNow,
        out DateTime expiresUtc);
}
