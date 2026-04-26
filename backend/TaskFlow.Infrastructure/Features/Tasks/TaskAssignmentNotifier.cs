using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks;

internal static class TaskAssignmentNotifier
{
    public static async System.Threading.Tasks.Task NotifyAssigneeAsync(
        TaskFlowDbContext dbContext,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        ILogger logger,
        Guid? actorUserId,
        DomainTask task,
        string projectName,
        ApplicationUser assignee,
        CancellationToken cancellationToken)
    {
        var baseUrl = emailSettings.Value.FrontendBaseUrl.TrimEnd('/');
        var taskUrl = $"{baseUrl}/tasks/{task.Id}";
        var assignerName = "Someone";
        if (actorUserId is { } aid)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == aid, cancellationToken);
            if (actor is not null)
            {
                assignerName = actor.DisplayName?.Trim() is { Length: > 0 } dn
                    ? dn
                    : actor.UserName ?? actor.Email ?? assignerName;
            }
        }

        await emailService.SendEmailAsync(
            assignee.Email ?? string.Empty,
            assignee.UserName ?? assignee.Email ?? string.Empty,
            $"You've been assigned: {task.Title}",
            EmailTemplates.TaskAssigned(
                assignee.UserName ?? assignee.Email ?? "there",
                task.Title,
                projectName,
                assignerName,
                taskUrl),
            "TaskAssigned",
            cancellationToken);

        logger.LogInformation(
            "Activity {ActivityType} TaskId={TaskId} AssigneeId={AssigneeId} ActorUserId={ActorUserId}",
            "task.assigned",
            task.Id,
            assignee.Id,
            actorUserId);
    }
}
