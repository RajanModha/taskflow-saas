using System.Collections.Concurrent;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Infrastructure.Projects;

public sealed class BoardCacheVersion : IBoardCacheVersion
{
    private readonly ConcurrentDictionary<Guid, long> _versions = new();

    public long GetSnapshot(Guid projectId) => _versions.GetOrAdd(projectId, _ => 0);

    public void BumpProject(Guid projectId) => _versions.AddOrUpdate(projectId, 1, (_, v) => v + 1);

    public void RemoveProject(Guid projectId) => _versions.TryRemove(projectId, out _);
}
