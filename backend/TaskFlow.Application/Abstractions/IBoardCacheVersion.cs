namespace TaskFlow.Application.Abstractions;

/// <summary>
/// Bumps a logical version for a project so cached board payloads can be treated as stale without prefix eviction.
/// </summary>
public interface IBoardCacheVersion
{
    long GetSnapshot(Guid projectId);

    void BumpProject(Guid projectId);

    /// <summary>Stops retaining a version counter for a deleted project (avoids dictionary growth).</summary>
    void RemoveProject(Guid projectId);
}
