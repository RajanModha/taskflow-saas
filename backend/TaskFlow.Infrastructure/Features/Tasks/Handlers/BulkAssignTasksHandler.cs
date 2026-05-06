using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class BulkAssignTasksHandler(
    ITaskBulkRepository taskRepository,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    IEmailService emailService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<BulkAssignTasksCommand, BulkTaskOperationResultDto>
{
    public async Task<BulkTaskOperationResultDto> Handle(BulkAssignTasksCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.BulkAssignTasksAsync(request.TaskIds, request.AssigneeId, cancellationToken);
        var failures = result.NotFound.Select(id => new BulkTaskFailureDto(id, "not_found")).ToList();
        if (result.InvalidAssignee)
        {
            return new BulkTaskOperationResultDto(0, [new BulkTaskFailureDto(Guid.Empty, "invalid_assignee")]);
        }

        if (request.AssigneeId is { } &&
            result.Mutated.Count > 0 &&
            result.AssigneeEmail is { Length: > 0 } toEmail)
        {
            var workspaceName = result.WorkspaceName ?? "your workspace";
            var frontendBaseUrl = emailSettings.Value.FrontendBaseUrl?.TrimEnd('/') ?? string.Empty;
            var body = EmailTemplates.BulkTaskAssignedSummary(
                result.AssigneeDisplayName ?? result.AssigneeUserName ?? string.Empty,
                result.Mutated.Count,
                workspaceName,
                $"{frontendBaseUrl}/tasks");
            await emailService.SendEmailAsync(
                toEmail,
                result.AssigneeDisplayName ?? result.AssigneeUserName ?? string.Empty,
                $"You have been assigned {result.Mutated.Count} tasks in {workspaceName}",
                body,
                "BulkTaskAssignedSummary",
                cancellationToken);
        }

        foreach (var task in result.Mutated)
        {
            DashboardCacheInvalidation.InvalidateAfterTaskMutation(
                cache,
                task.OrganizationId,
                currentUser.UserId,
                task.PreviousAssigneeId,
                task.CurrentAssigneeId);
            boardCacheVersion.BumpProject(task.ProjectId);
        }

        return new BulkTaskOperationResultDto(result.Mutated.Count, failures);
    }
}
