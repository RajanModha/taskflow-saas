using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class BulkAssignTasksHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    IEmailService emailService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<BulkAssignTasksCommand, BulkTaskOperationResultDto>
{
    public async Task<BulkTaskOperationResultDto> Handle(BulkAssignTasksCommand request, CancellationToken cancellationToken)
    {
        var ids = request.TaskIds.Distinct().ToArray();
        var tasks = await dbContext.Tasks
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var failures = new List<BulkTaskFailureDto>();
        var foundIds = tasks.Select(t => t.Id).ToHashSet();
        failures.AddRange(ids.Where(id => !foundIds.Contains(id)).Select(id => new BulkTaskFailureDto(id, "not_found")));
        var changedTasks = tasks.Where(t => t.AssigneeId != request.AssigneeId).ToList();

        ApplicationUser? assignee = null;
        if (request.AssigneeId is { } assigneeId)
        {
            assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
            if (assignee is null || tasks.Any(t => t.OrganizationId != assignee.OrganizationId))
            {
                return new BulkTaskOperationResultDto(0, [new BulkTaskFailureDto(Guid.Empty, "invalid_assignee")]);
            }
        }

        var now = DateTime.UtcNow;
        foreach (var task in changedTasks)
        {
            task.AssigneeId = request.AssigneeId;
            task.UpdatedAtUtc = now;
            task.ReminderSent = false;
        }

        if (changedTasks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (request.AssigneeId is { } && assignee is not null && changedTasks.Count > 0)
        {
            var workspaceName = await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == assignee.OrganizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "your workspace";
            var frontendBaseUrl = emailSettings.Value.FrontendBaseUrl?.TrimEnd('/') ?? string.Empty;
            var body = EmailTemplates.BulkTaskAssignedSummary(
                assignee.DisplayName ?? assignee.UserName ?? string.Empty,
                changedTasks.Count,
                workspaceName,
                $"{frontendBaseUrl}/tasks");
            await emailService.SendEmailAsync(
                assignee.Email ?? string.Empty,
                assignee.DisplayName ?? assignee.UserName ?? string.Empty,
                $"You have been assigned {changedTasks.Count} tasks in {workspaceName}",
                body,
                "BulkTaskAssignedSummary",
                cancellationToken);
        }

        foreach (var task in changedTasks)
        {
            DashboardCacheInvalidation.InvalidateAfterTaskMutation(
                cache,
                task.OrganizationId,
                currentUser.UserId,
                null,
                task.AssigneeId);
            boardCacheVersion.BumpProject(task.ProjectId);
        }

        return new BulkTaskOperationResultDto(changedTasks.Count, failures);
    }
}
