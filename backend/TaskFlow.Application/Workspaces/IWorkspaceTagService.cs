using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Workspaces;

public sealed record CreateWorkspaceTagRequest(string Name, string Color);

public sealed record UpdateWorkspaceTagRequest(string? Name, string? Color);

public interface IWorkspaceTagService
{
    Task<IReadOnlyList<TagDto>?> ListTagsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<(int StatusCode, object? Body)> CreateTagAsync(
        Guid actorUserId,
        CreateWorkspaceTagRequest request,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, object? Body)> UpdateTagAsync(
        Guid actorUserId,
        Guid tagId,
        UpdateWorkspaceTagRequest request,
        CancellationToken cancellationToken = default);

    Task<int> DeleteTagAsync(Guid actorUserId, Guid tagId, CancellationToken cancellationToken = default);
}
