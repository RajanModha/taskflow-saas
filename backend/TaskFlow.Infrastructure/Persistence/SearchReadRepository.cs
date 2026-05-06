using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Search;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class SearchReadRepository(TaskFlowDbContext dbContext) : ISearchReadRepository
{
    public async Task<SearchResultReadModel> SearchWorkspaceAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var pattern = $"%{query.Replace("%", string.Empty).Replace("_", string.Empty)}%";

        var taskRows = await dbContext.Tasks
            .AsNoTracking()
            .Where(t =>
                EF.Functions.Like(t.Title, pattern) ||
                (t.Description != null && EF.Functions.Like(t.Description, pattern)))
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.ProjectId,
                Score = t.Title.Equals(query) ? 5 :
                    EF.Functions.Like(t.Title, pattern) ? 3 :
                    (t.Description != null && EF.Functions.Like(t.Description, pattern) ? 2 : 0),
            })
            .OrderByDescending(t => t.Score)
            .ThenBy(t => t.Title)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var projectNameById = await dbContext.Projects
            .AsNoTracking()
            .Where(p => taskRows.Select(t => t.ProjectId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var taskHits = taskRows
            .Select(t => new SearchHitReadModel(
                t.Id,
                "task",
                t.Title,
                Snippet($"{t.Title} {t.Description}".Trim(), query),
                t.Score,
                new
                {
                    status = t.Status.ToString(),
                    priority = t.Priority.ToString(),
                    projectName = projectNameById.TryGetValue(t.ProjectId, out var projectName) ? projectName : string.Empty,
                }))
            .ToList();

        var projectRows = await dbContext.Projects
            .AsNoTracking()
            .Where(p =>
                EF.Functions.Like(p.Name, pattern) ||
                (p.Description != null && EF.Functions.Like(p.Description, pattern)))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                Score = p.Name.Equals(query) ? 5 :
                    EF.Functions.Like(p.Name, pattern) ? 3 :
                    (p.Description != null && EF.Functions.Like(p.Description, pattern) ? 2 : 0),
            })
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var projectTaskCounts = await dbContext.Tasks
            .AsNoTracking()
            .Where(t => projectRows.Select(p => p.Id).Contains(t.ProjectId))
            .GroupBy(t => t.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, cancellationToken);

        var projectHits = projectRows
            .Select(p => new SearchHitReadModel(
                p.Id,
                "project",
                p.Name,
                Snippet($"{p.Name} {p.Description}".Trim(), query),
                p.Score,
                new
                {
                    taskCount = projectTaskCounts.TryGetValue(p.Id, out var taskCount) ? taskCount : 0,
                }))
            .ToList();

        var commentRows = await dbContext.Comments
            .AsNoTracking()
            .Where(c => !c.IsDeleted && EF.Functions.Like(c.Content, pattern))
            .Select(c => new
            {
                c.Id,
                c.Content,
                Score = EF.Functions.Like(c.Content, pattern) ? 1 : 0,
            })
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var commentHits = commentRows
            .Select(c => new SearchHitReadModel(
                c.Id,
                "comment",
                Snippet(c.Content, query, 60),
                Snippet(c.Content, query),
                c.Score,
                new { }))
            .ToList();

        return new SearchResultReadModel(
            query,
            taskHits.Count + projectHits.Count + commentHits.Count,
            taskHits,
            projectHits,
            commentHits);
    }

    private static string Snippet(string? text, string query, int maxLen = 120)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return text[..Math.Min(maxLen, text.Length)] + (text.Length > maxLen ? "..." : string.Empty);
        }

        var start = Math.Max(0, idx - 40);
        var end = Math.Min(text.Length, start + maxLen);
        return (start > 0 ? "..." : string.Empty) +
               text[start..end] +
               (end < text.Length ? "..." : string.Empty);
    }
}
