using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks;

public sealed class ReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderHostedService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        await RunOnceAsync(stoppingToken);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async System.Threading.Tasks.Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var emailSettings = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var windowEnd = now.AddHours(24);

            var tasks = await dbContext.Tasks
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t =>
                    t.AssigneeId != null &&
                    !t.ReminderSent &&
                    t.DueDateUtc != null &&
                    t.DueDateUtc >= now &&
                    t.DueDateUtc <= windowEnd &&
                    t.Status != TaskFlow.Domain.Entities.TaskStatus.Done &&
                    t.Status != TaskFlow.Domain.Entities.TaskStatus.Cancelled)
                .ToListAsync(cancellationToken);

            if (tasks.Count == 0)
            {
                return;
            }

            var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();
            var orgIds = tasks.Select(t => t.OrganizationId).Distinct().ToList();

            var projects = await dbContext.Projects
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => orgIds.Contains(p.OrganizationId) && projectIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var projectByKey = projects.ToDictionary(p => (p.Id, p.OrganizationId));

            var assigneeIds = tasks.Select(t => t.AssigneeId!.Value).Distinct().ToList();
            var assignees = await dbContext.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => assigneeIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            var baseUrl = emailSettings.Value.FrontendBaseUrl.TrimEnd('/');
            var sentIds = new List<Guid>();

            foreach (var task in tasks)
            {
                if (!projectByKey.TryGetValue((task.ProjectId, task.OrganizationId), out var project))
                {
                    logger.LogWarning("Skipping reminder for task {TaskId}: project not found.", task.Id);
                    continue;
                }

                if (!assignees.TryGetValue(task.AssigneeId!.Value, out var assignee) ||
                    assignee.OrganizationId != task.OrganizationId ||
                    string.IsNullOrEmpty(assignee.Email))
                {
                    logger.LogWarning("Skipping reminder for task {TaskId}: assignee missing or invalid.", task.Id);
                    continue;
                }

                var taskUrl = $"{baseUrl}/tasks/{task.Id}";
                var dueLabel = task.DueDateUtc!.Value.ToString("MMM dd, yyyy h:mm tt UTC");

                await emailService.SendEmailAsync(
                    assignee.Email,
                    assignee.UserName ?? assignee.Email,
                    $"Task due soon: {task.Title}",
                    EmailTemplates.DueDateReminder(
                        assignee.UserName ?? assignee.Email,
                        task.Title,
                        project.Name,
                        dueLabel,
                        taskUrl),
                    "TaskDueReminder",
                    cancellationToken);

                sentIds.Add(task.Id);
            }

            if (sentIds.Count > 0)
            {
                await dbContext.Tasks
                    .IgnoreQueryFilters()
                    .Where(t => sentIds.Contains(t.Id))
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(t => t.ReminderSent, true),
                        cancellationToken);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Due-date reminder job failed.");
        }
    }
}
