using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Entities;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

internal static class MilestoneProgress
{
    internal static decimal Progress(int total, int completed)
    {
        if (total == 0)
        {
            return 0;
        }

        return Math.Round((decimal)completed * 100m / total, 1, MidpointRounding.AwayFromZero);
    }
}

public sealed class GetMilestonesQueryHandler(TaskFlowDbContext dbContext)
    : IRequestHandler<GetMilestonesQuery, IReadOnlyList<MilestoneDto>?>
{
    public async Task<IReadOnlyList<MilestoneDto>?> Handle(GetMilestonesQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return null;
        }

        var milestones = await dbContext.Milestones
            .AsNoTracking()
            .Where(m => m.ProjectId == request.ProjectId)
            .OrderBy(m => m.DueDateUtc ?? DateTime.MaxValue)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        if (milestones.Count == 0)
        {
            return [];
        }

        var ids = milestones.Select(m => m.Id).ToList();
        var stats = await dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.MilestoneId != null && ids.Contains(t.MilestoneId.Value) && !t.IsDeleted)
            .GroupBy(t => t.MilestoneId!.Value)
            .Select(g => new
            {
                MilestoneId = g.Key,
                Total = g.Count(),
                Completed = g.Count(x => x.Status == DomainTaskStatus.Done),
            })
            .ToDictionaryAsync(x => x.MilestoneId, x => (x.Total, x.Completed), cancellationToken);

        return milestones
            .Select(m =>
            {
                stats.TryGetValue(m.Id, out var s);
                var total = s.Total;
                var completed = s.Completed;
                return new MilestoneDto(
                    m.Id,
                    m.ProjectId,
                    m.Name,
                    m.Description,
                    m.DueDateUtc,
                    total,
                    completed,
                    MilestoneProgress.Progress(total, completed),
                    m.CreatedAtUtc);
            })
            .ToList();
    }
}

public sealed class CreateMilestoneCommandHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<CreateMilestoneCommand, MilestoneDto?>
{
    public async Task<MilestoneDto?> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var milestone = new Milestone
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentTenant.OrganizationId,
            ProjectId = request.ProjectId,
            Name = request.Name,
            Description = request.Description,
            DueDateUtc = request.DueDateUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        await dbContext.Milestones.AddAsync(milestone, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        boardCacheVersion.BumpProject(request.ProjectId);

        return new MilestoneDto(
            milestone.Id,
            milestone.ProjectId,
            milestone.Name,
            milestone.Description,
            milestone.DueDateUtc,
            0,
            0,
            0,
            milestone.CreatedAtUtc);
    }
}

public sealed class UpdateMilestoneCommandHandler(
    TaskFlowDbContext dbContext,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<UpdateMilestoneCommand, MilestoneDto?>
{
    public async Task<MilestoneDto?> Handle(UpdateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var milestone = await dbContext.Milestones
            .FirstOrDefaultAsync(
                m => m.Id == request.MilestoneId && m.ProjectId == request.ProjectId,
                cancellationToken);

        if (milestone is null)
        {
            return null;
        }

        milestone.Name = request.Name;
        milestone.Description = request.Description;
        milestone.DueDateUtc = request.DueDateUtc;
        milestone.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        boardCacheVersion.BumpProject(request.ProjectId);

        var total = await dbContext.Tasks
            .AsNoTracking()
            .CountAsync(t => t.MilestoneId == milestone.Id && !t.IsDeleted, cancellationToken);
        var completed = await dbContext.Tasks
            .AsNoTracking()
            .CountAsync(
                t => t.MilestoneId == milestone.Id && !t.IsDeleted && t.Status == DomainTaskStatus.Done,
                cancellationToken);
        return new MilestoneDto(
            milestone.Id,
            milestone.ProjectId,
            milestone.Name,
            milestone.Description,
            milestone.DueDateUtc,
            total,
            completed,
            MilestoneProgress.Progress(total, completed),
            milestone.CreatedAtUtc);
    }
}

public sealed class DeleteMilestoneCommandHandler(
    TaskFlowDbContext dbContext,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<DeleteMilestoneCommand, bool>
{
    public async Task<bool> Handle(DeleteMilestoneCommand request, CancellationToken cancellationToken)
    {
        var milestone = await dbContext.Milestones
            .FirstOrDefaultAsync(
                m => m.Id == request.MilestoneId && m.ProjectId == request.ProjectId,
                cancellationToken);

        if (milestone is null)
        {
            return false;
        }

        await dbContext.Tasks
            .IgnoreQueryFilters()
            .Where(t => t.MilestoneId == milestone.Id && t.OrganizationId == milestone.OrganizationId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.MilestoneId, (Guid?)null),
                cancellationToken);

        milestone.IsDeleted = true;
        milestone.DeletedAt = DateTime.UtcNow;
        milestone.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        boardCacheVersion.BumpProject(request.ProjectId);
        return true;
    }
}
