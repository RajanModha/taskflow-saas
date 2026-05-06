using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Search;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Search.Handlers;

public sealed class GetWorkspaceSearchHandler(
    ISearchReadRepository searchReadRepository,
    ICurrentTenant currentTenant,
    IMemoryCache cache)
    : IRequestHandler<GetWorkspaceSearchQuery, SearchResultDto>
{
    public async Task<SearchResultDto> Handle(GetWorkspaceSearchQuery request, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return new SearchResultDto(request.Query, 0, [], [], []);
        }

        var query = request.Query.Trim();
        var limit = request.Limit is < 1 or > 20 ? 5 : request.Limit;
        var cacheKey = $"search:{currentTenant.OrganizationId}:{query.ToLowerInvariant()}:{limit}";
        if (cache.TryGetValue(cacheKey, out SearchResultDto? cached) && cached is not null)
        {
            return cached;
        }

        var result = await searchReadRepository.SearchWorkspaceAsync(query, limit, cancellationToken);
        var dto = new SearchResultDto(
            result.Query,
            result.Total,
            result.Tasks.Select(h => new SearchHitDto(h.Id, h.Type, h.Title, h.Snippet, h.Score, h.Metadata)).ToList(),
            result.Projects.Select(h => new SearchHitDto(h.Id, h.Type, h.Title, h.Snippet, h.Score, h.Metadata)).ToList(),
            result.Comments.Select(h => new SearchHitDto(h.Id, h.Type, h.Title, h.Snippet, h.Score, h.Metadata)).ToList());
        cache.Set(cacheKey, dto, TimeSpan.FromSeconds(30));
        return dto;
    }
}
