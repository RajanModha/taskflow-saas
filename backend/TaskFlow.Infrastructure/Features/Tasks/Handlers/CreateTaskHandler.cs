using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class CreateTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger) : IRequestHandler<CreateTaskCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
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

        ApplicationUser? assignee = null;
        if (request.AssigneeId is { } assigneeId)
        {
            assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != currentTenant.OrganizationId)
            {
                return null;
            }
        }

        if (request.TagIds is { Length: > 0 } tagIdsForValidate &&
            !await TaskTagging.ValidateTagIdsInOrganizationAsync(
                dbContext,
                currentTenant.OrganizationId,
                tagIdsForValidate,
                cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentTenant.OrganizationId,
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            DueDateUtc = request.DueDateUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            AssigneeId = request.AssigneeId,
            ReminderSent = false,
        };

        await dbContext.Tasks.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await TaskTagging.ReplaceTaskTagsAsync(dbContext, task.Id, request.TagIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (assignee is not null)
        {
            await TaskAssignmentNotifier.NotifyAssigneeAsync(
                dbContext,
                notificationService,
                emailSettings,
                currentUser.UserId,
                task,
                project.Name,
                assignee,
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, currentTenant.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, request.AssigneeId);
        boardCacheVersion.BumpProject(task.ProjectId);

        if (currentUser.UserId is { } creatorId)
        {
            var creator = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == creatorId, cancellationToken);
            var actorName = creator?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                task.Id,
                ActivityActions.TaskCreated,
                creatorId,
                actorName,
                currentTenant.OrganizationId,
                new { projectId = task.ProjectId, title = task.Title },
                cancellationToken);
        }

        var dtoList = await TaskProjection.ToDtosAsync(dbContext, [task], cancellationToken);
        return dtoList[0];
    }
}

