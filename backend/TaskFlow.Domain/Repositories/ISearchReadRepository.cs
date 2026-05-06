namespace TaskFlow.Domain.Repositories;

public interface ISearchReadRepository
{
    Task<SearchResultReadModel> SearchWorkspaceAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
}
