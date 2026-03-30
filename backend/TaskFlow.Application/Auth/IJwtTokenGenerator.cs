namespace TaskFlow.Application.Auth;

public interface IJwtTokenGenerator
{
    string CreateAccessToken(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        Guid organizationId,
        DateTime utcNow,
        out DateTime expiresUtc);
}
