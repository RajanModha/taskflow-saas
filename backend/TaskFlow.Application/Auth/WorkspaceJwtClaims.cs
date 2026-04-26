namespace TaskFlow.Application.Auth;

/// <summary>JWT claim used for workspace RBAC (Owner / Admin / Member).</summary>
public static class WorkspaceJwtClaims
{
    public const string Role = "workspace_role";
}
