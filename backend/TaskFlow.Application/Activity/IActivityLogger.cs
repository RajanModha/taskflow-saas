namespace TaskFlow.Application.Activity;

public interface IActivityLogger
{
    /// <summary>
    /// Persists an activity row. Implementations should not throw to callers and should not block the main request.
    /// </summary>
    /// <param name="organizationId">Workspace scope for tenant filtering on reads.</param>
    Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid actorId,
        string actorName,
        Guid organizationId,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
