using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class CreateTaskFromTemplateHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    TimeProvider timeProvider,
    IActivityLogger activityLogger,
    IWebhookDispatcher webhookDispatcher)
    : IRequestHandler<CreateTaskFromTemplateCommand, TaskDto?>
{
    public async Task<TaskDto?> Handle(CreateTaskFromTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var orgId = currentTenant.OrganizationId;

        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == request.ProjectId && p.OrganizationId == orgId,
                cancellationToken);
        if (project is null)
        {
            return null;
        }

        var template = await dbContext.TaskTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == request.TemplateId && t.OrganizationId == orgId,
                cancellationToken);
        if (template is null)
        {
            return null;
        }

        ApplicationUser? assignee = null;
        if (request.Overrides?.AssigneeId is { } assigneeId)
        {
            assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != currentTenant.OrganizationId)
            {
                return null;
            }
        }

        var dueDate = ResolveDueDateUtc(request.Overrides, template, timeProvider.GetUtcNow().UtcDateTime);
        var title = string.IsNullOrWhiteSpace(request.Overrides?.Title)
            ? template.DefaultTitle
            : request.Overrides.Title!.Trim();
        var description = request.Overrides?.Description ?? template.DefaultDescription;
        var priority = request.Overrides?.Priority ?? template.DefaultPriority;

        var now = DateTime.UtcNow;
        var task = new DomainTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ProjectId = request.ProjectId,
            Title = title,
            Description = description,
            Status = DomainTaskStatus.Todo,
            Priority = priority,
            DueDateUtc = dueDate,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            AssigneeId = request.Overrides?.AssigneeId,
            ReminderSent = false,
            TemplateId = template.Id,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Tasks.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var templateChecklist = await dbContext.TaskTemplateChecklistItems
            .AsNoTracking()
            .Where(i => i.TemplateId == template.Id)
            .OrderBy(i => i.Order)
            .ToListAsync(cancellationToken);
        if (templateChecklist.Count > 0)
        {
            var checklistItems = templateChecklist
                .Select(i => new ChecklistItem
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    Title = i.Title,
                    Order = i.Order,
                    IsCompleted = false,
                    CreatedAtUtc = now,
                    CompletedAtUtc = null,
                })
                .ToList();
            await dbContext.ChecklistItems.AddRangeAsync(checklistItems, cancellationToken);
        }

        var templateTagIds = await dbContext.TaskTemplateTags
            .AsNoTracking()
            .Where(tt => tt.TemplateId == template.Id)
            .Select(tt => tt.TagId)
            .ToArrayAsync(cancellationToken);
        await TaskTagging.ReplaceTaskTagsAsync(dbContext, task.Id, templateTagIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

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

        if (currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                task.Id,
                ActivityActions.TaskCreatedFromTemplate,
                actorId,
                actorName,
                orgId,
                new { templateName = template.Name, projectId = task.ProjectId, title = task.Title },
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, orgId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, task.AssigneeId);
        boardCacheVersion.BumpProject(task.ProjectId);

        var dtoList = await TaskProjection.ToDtosAsync(dbContext, [task], cancellationToken);

        await webhookDispatcher.DispatchOrganizationEventAsync(
            orgId,
            WebhookEventTypes.TaskCreated,
            new { taskId = task.Id, projectId = task.ProjectId, title = task.Title },
            cancellationToken);

        return dtoList[0];
    }

    private static DateTime? ResolveDueDateUtc(
        CreateTaskFromTemplateOverrides? overrides,
        TaskTemplate template,
        DateTime nowUtc)
    {
        if (overrides?.DueDateUtc is { } overrideDue)
        {
            return overrideDue;
        }

        if (template.DefaultDueDaysFromNow is { } dueDays)
        {
            return nowUtc.AddDays(dueDays);
        }

        return null;
    }
}
