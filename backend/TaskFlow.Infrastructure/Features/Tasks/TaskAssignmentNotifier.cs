using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks;

internal static class TaskAssignmentNotifier
{
    public static async System.Threading.Tasks.Task NotifyAssigneeAsync(
        TaskFlowDbContext dbContext,
        INotificationService notificationService,
        IOptions<EmailSettings> emailSettings,
        Guid? actorUserId,
        DomainTask task,
        string projectName,
        ApplicationUser assignee,
        CancellationToken cancellationToken)
    {
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

        var assigneeName = assignee.DisplayName?.Trim() is { Length: > 0 } display
            ? display
            : assignee.UserName ?? assignee.Email ?? "there";

        var title = "Task assigned";
        var body = $"{assignerName} assigned you '{task.Title}'";

        var baseUrl = emailSettings.Value.FrontendBaseUrl.TrimEnd('/');
        var taskUrl = $"{baseUrl}/tasks/{task.Id}";

        await notificationService.CreateAsync(
            assignee.Id,
            "task.assigned",
            title,
            body,
            entityType: "Task",
            entityId: task.Id,
            sendEmail: true,
            toEmail: assignee.Email,
            emailSubject: $"You've been assigned: {task.Title}",
            emailHtml: EmailTemplates.TaskAssigned(
                assigneeName,
                task.Title,
                projectName,
                assignerName,
                taskUrl),
            ct: cancellationToken);
    }
}
