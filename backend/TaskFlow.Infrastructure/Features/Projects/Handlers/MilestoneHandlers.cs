using MediatR;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

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

public sealed class GetMilestonesQueryHandler(IProjectReadRepository projectReadRepository)
    : IRequestHandler<GetMilestonesQuery, IReadOnlyList<MilestoneDto>?>
{
    public async Task<IReadOnlyList<MilestoneDto>?> Handle(GetMilestonesQuery request, CancellationToken cancellationToken)
    {
        var milestones = await projectReadRepository.GetProjectMilestonesAsync(request.ProjectId, cancellationToken);
        if (milestones is null)
        {
            return null;
        }

        if (milestones.Count == 0)
        {
            return [];
        }

        var ids = milestones.Select(m => m.Id).ToList();
        var stats = await projectReadRepository.GetMilestoneStatsAsync(ids, cancellationToken);

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
    IProjectWriteRepository projectWriteRepository,
    IProjectReadRepository projectReadRepository,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<CreateMilestoneCommand, MilestoneDto?>
{
    public async Task<MilestoneDto?> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var created = await projectWriteRepository.CreateMilestoneAsync(
            request.ProjectId,
            request.Name,
            request.Description,
            request.DueDateUtc,
            cancellationToken);
        if (created is null)
        {
            return null;
        }
        boardCacheVersion.BumpProject(request.ProjectId);

        var milestones = await projectReadRepository.GetProjectMilestonesAsync(request.ProjectId, cancellationToken) ?? [];
        var milestone = milestones.FirstOrDefault(m => m.Id == created.MilestoneId);
        if (milestone is null)
        {
            return null;
        }
        return new MilestoneDto(
            created.MilestoneId,
            request.ProjectId,
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
    IProjectWriteRepository projectWriteRepository,
    IProjectReadRepository projectReadRepository,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<UpdateMilestoneCommand, MilestoneDto?>
{
    public async Task<MilestoneDto?> Handle(UpdateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var updated = await projectWriteRepository.UpdateMilestoneAsync(
            request.ProjectId,
            request.MilestoneId,
            request.Name,
            request.Description,
            request.DueDateUtc,
            cancellationToken);
        if (updated is null)
        {
            return null;
        }
        boardCacheVersion.BumpProject(request.ProjectId);

        var milestones = await projectReadRepository.GetProjectMilestonesAsync(request.ProjectId, cancellationToken) ?? [];
        var milestone = milestones.FirstOrDefault(m => m.Id == request.MilestoneId);
        if (milestone is null)
        {
            return null;
        }
        var stats = await projectReadRepository.GetMilestoneStatsAsync([request.MilestoneId], cancellationToken);
        stats.TryGetValue(request.MilestoneId, out var s);
        var total = s.Total;
        var completed = s.Completed;
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
    IProjectWriteRepository projectWriteRepository,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<DeleteMilestoneCommand, bool>
{
    public async Task<bool> Handle(DeleteMilestoneCommand request, CancellationToken cancellationToken)
    {
        var deleted = await projectWriteRepository.DeleteMilestoneAsync(
            request.ProjectId,
            request.MilestoneId,
            cancellationToken);
        if (deleted is null)
        {
            return false;
        }
        boardCacheVersion.BumpProject(request.ProjectId);
        return true;
    }
}
