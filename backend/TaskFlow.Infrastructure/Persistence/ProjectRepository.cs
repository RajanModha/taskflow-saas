using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Repositories;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class ProjectRepository(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant) : IProjectReadRepository, IProjectWriteRepository
{
    public async Task<PagedResult<ProjectReadModel>> GetPagedProjectsAsync(
        ProjectListCriteria criteria,
        CancellationToken cancellationToken)
    {
        var page = criteria.Page < 1 ? 1 : criteria.Page;
        var pageSize = criteria.PageSize is < 1 or > 100 ? 20 : criteria.PageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.Projects.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(criteria.Q))
        {
            var q = criteria.Q.Trim();
            query = query.Where(p => p.Name.Contains(q));
        }

        query = criteria.SortBy?.Trim().ToLowerInvariant() switch
        {
            null or "" or "createdatutc" => criteria.SortDesc ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
            "name" => criteria.SortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => criteria.SortDesc ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
        };

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize)
            .Select(p => new ProjectReadModel(p.Id, p.Name, p.Description, p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedResult<ProjectReadModel>(items, page, pageSize, total);
    }

    public async Task<ProjectReadModel?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new ProjectReadModel(p.Id, p.Name, p.Description, p.CreatedAtUtc, p.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<ProjectActivityRow>?> GetProjectActivityAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var projectExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            return null;
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.ActivityLogs
            .AsNoTracking()
            .Where(a =>
                (a.EntityType == TaskFlow.Application.Activity.ActivityEntityTypes.Project && a.EntityId == projectId) ||
                (a.EntityType == TaskFlow.Application.Activity.ActivityEntityTypes.Task &&
                 dbContext.Tasks.Any(t => t.Id == a.EntityId && t.ProjectId == projectId)));

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .Select(a => new ProjectActivityRow(a.Id, a.Action, a.ActorId, a.ActorName, a.OccurredAtUtc, a.Metadata))
            .ToListAsync(cancellationToken);
        return new PagedResult<ProjectActivityRow>(rows, page, pageSize, total);
    }

    public async Task<IReadOnlyList<MilestoneReadModel>?> GetProjectMilestonesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await dbContext.Milestones
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.DueDateUtc ?? DateTime.MaxValue)
            .ThenBy(m => m.Name)
            .Select(m => new MilestoneReadModel(m.Id, m.ProjectId, m.Name, m.Description, m.DueDateUtc, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, (int Total, int Completed)>> GetMilestoneStatsAsync(
        IReadOnlyList<Guid> milestoneIds,
        CancellationToken cancellationToken)
    {
        if (milestoneIds.Count == 0)
        {
            return new Dictionary<Guid, (int Total, int Completed)>();
        }

        return await dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.MilestoneId != null && milestoneIds.Contains(t.MilestoneId.Value) && !t.IsDeleted)
            .GroupBy(t => t.MilestoneId!.Value)
            .Select(g => new
            {
                MilestoneId = g.Key,
                Total = g.Count(),
                Completed = g.Count(x => x.Status == DomainTaskStatus.Done),
            })
            .ToDictionaryAsync(x => x.MilestoneId, x => (x.Total, x.Completed), cancellationToken);
    }

    public async Task<ProjectBoardTaskReadModel?> GetBoardTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        string? assigneeUserName = null;
        string? assigneeDisplayName = null;
        if (task.AssigneeId is { } assigneeId)
        {
            var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
            assigneeUserName = user?.UserName;
            assigneeDisplayName = user?.DisplayName;
        }

        var commentCount = await dbContext.Comments
            .AsNoTracking()
            .CountAsync(c => c.TaskId == taskId && !c.IsDeleted, cancellationToken);

        var tags = task.TaskTags
            .Where(tt => tt.Tag is not null)
            .Select(tt => (tt.Tag!.Id, tt.Tag.Name, tt.Tag.Color))
            .ToList();

        return new ProjectBoardTaskReadModel(
            task.Id,
            task.Title,
            task.Status,
            task.Priority,
            task.DueDateUtc,
            task.AssigneeId,
            assigneeUserName,
            assigneeDisplayName,
            tags,
            commentCount,
            task.CreatedAtUtc);
    }

    public async Task<(ProjectReadModel Project, IReadOnlyList<ProjectBoardTaskReadModel> Tasks)?> GetProjectBoardDataAsync(
        Guid projectId,
        Guid? assigneeId,
        Guid? tagId,
        string? q,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId);

        if (assigneeId.HasValue)
        {
            query = query.Where(t => t.AssigneeId == assigneeId.Value);
        }

        if (tagId.HasValue)
        {
            var filterTagId = tagId.Value;
            query = query.Where(t => dbContext.TaskTags.Any(tt => tt.TaskId == t.Id && tt.TagId == filterTagId));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qNorm = q.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(qNorm));
        }

        var tasks = await query
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        if (tasks.Count == 0)
        {
            return (project, []);
        }

        var taskIds = tasks.Select(t => t.Id).ToList();
        var commentCounts = await dbContext.Comments
            .AsNoTracking()
            .Where(c => taskIds.Contains(c.TaskId) && !c.IsDeleted)
            .GroupBy(c => c.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TaskId, x => x.Count, cancellationToken);

        var assigneeIds = tasks.Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value).Distinct().ToList();
        var assigneeLookup = new Dictionary<Guid, (string? UserName, string? DisplayName)>();
        if (assigneeIds.Count > 0)
        {
            assigneeLookup = await dbContext.Users
                .AsNoTracking()
                .Where(u => assigneeIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => (u.UserName, u.DisplayName), cancellationToken);
        }

        var mapped = tasks.Select(t =>
        {
            assigneeLookup.TryGetValue(t.AssigneeId ?? Guid.Empty, out var assignee);
            commentCounts.TryGetValue(t.Id, out var commentCount);
            var tags = t.TaskTags
                .Where(tt => tt.Tag is not null)
                .Select(tt => (tt.Tag!.Id, tt.Tag.Name, tt.Tag.Color))
                .ToList();
            return new ProjectBoardTaskReadModel(
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.DueDateUtc,
                t.AssigneeId,
                assignee.UserName,
                assignee.DisplayName,
                tags,
                commentCount,
                t.CreatedAtUtc);
        }).ToList();

        return (project, mapped);
    }

    public async Task<(ProjectReadModel Project, IReadOnlyList<TaskFlow.Domain.Entities.Task> Tasks)?> GetProjectExportDataAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return (project, tasks);
    }

    public async Task<CreateProjectMutationResult> CreateProjectAsync(
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var now = DateTime.UtcNow;
        var project = new Domain.Entities.Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentTenant.OrganizationId,
            Name = name,
            Description = description,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        await dbContext.Projects.AddAsync(project, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreateProjectMutationResult(
            project.Id,
            project.Name,
            project.OrganizationId,
            project.CreatedAtUtc,
            project.UpdatedAtUtc,
            project.Description);
    }

    public async Task<UpdateProjectMutationResult?> UpdateProjectAsync(
        Guid projectId,
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var previousName = project.Name;
        var previousDescription = project.Description;
        project.Name = name;
        project.Description = description;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateProjectMutationResult(
            project.Id,
            project.OrganizationId,
            previousName,
            previousDescription,
            project.Name,
            project.Description);
    }

    public async Task<DeleteProjectMutationResult?> SoftDeleteProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        project.IsDeleted = true;
        project.DeletedAt = now;
        project.UpdatedAtUtc = now;

        var projectTasks = await dbContext.Tasks
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var task in projectTasks)
        {
            task.IsDeleted = true;
            task.DeletedAt = now;
            task.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteProjectMutationResult(project.Id, project.OrganizationId, project.Name, true);
    }

    public async Task<RestoreProjectMutationResult?> RestoreProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return null;
        }

        var project = await dbContext.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.Id == projectId && p.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
        if (project is null || !project.IsDeleted)
        {
            return null;
        }

        project.IsDeleted = false;
        project.DeletedAt = null;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RestoreProjectMutationResult(project.Id, project.OrganizationId, project.Name);
    }

    public async Task<MilestoneMutationResult?> CreateMilestoneAsync(
        Guid projectId,
        string name,
        string? description,
        DateTime? dueDateUtc,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var milestone = new TaskFlow.Domain.Entities.Milestone
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentTenant.OrganizationId,
            ProjectId = projectId,
            Name = name,
            Description = description,
            DueDateUtc = dueDateUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        await dbContext.Milestones.AddAsync(milestone, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new MilestoneMutationResult(true, milestone.Id, milestone.ProjectId, milestone.OrganizationId);
    }

    public async Task<MilestoneMutationResult?> UpdateMilestoneAsync(
        Guid projectId,
        Guid milestoneId,
        string name,
        string? description,
        DateTime? dueDateUtc,
        CancellationToken cancellationToken)
    {
        var milestone = await dbContext.Milestones
            .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ProjectId == projectId, cancellationToken);
        if (milestone is null)
        {
            return null;
        }

        milestone.Name = name;
        milestone.Description = description;
        milestone.DueDateUtc = dueDateUtc;
        milestone.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new MilestoneMutationResult(true, milestone.Id, milestone.ProjectId, milestone.OrganizationId);
    }

    public async Task<MilestoneMutationResult?> DeleteMilestoneAsync(
        Guid projectId,
        Guid milestoneId,
        CancellationToken cancellationToken)
    {
        var milestone = await dbContext.Milestones
            .FirstOrDefaultAsync(m => m.Id == milestoneId && m.ProjectId == projectId, cancellationToken);
        if (milestone is null)
        {
            return null;
        }

        await dbContext.Tasks
            .IgnoreQueryFilters()
            .Where(t => t.MilestoneId == milestone.Id && t.OrganizationId == milestone.OrganizationId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.MilestoneId, (Guid?)null), cancellationToken);

        milestone.IsDeleted = true;
        milestone.DeletedAt = DateTime.UtcNow;
        milestone.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new MilestoneMutationResult(true, milestone.Id, milestone.ProjectId, milestone.OrganizationId);
    }

    public async Task<MoveProjectBoardTaskMutationResult?> MoveProjectBoardTaskAsync(
        Guid projectId,
        Guid taskId,
        DomainTaskStatus newStatus,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var previousStatus = task.Status;
        var changed = task.Status != newStatus;
        if (changed)
        {
            task.Status = newStatus;
            task.UpdatedAtUtc = nowUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new MoveProjectBoardTaskMutationResult(
            true,
            changed,
            task.Id,
            task.OrganizationId,
            task.ProjectId,
            previousStatus,
            task.AssigneeId);
    }
}
