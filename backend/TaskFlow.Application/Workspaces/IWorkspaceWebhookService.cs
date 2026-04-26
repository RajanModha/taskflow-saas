using TaskFlow.Application.Common;

namespace TaskFlow.Application.Workspaces;

public interface IWorkspaceWebhookService
{
    Task<IReadOnlyList<WebhookDto>?> ListWebhooksAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<(int StatusCode, object? Body)> CreateWebhookAsync(
        Guid actorUserId,
        CreateWorkspaceWebhookRequest request,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, object? Body)> UpdateWebhookAsync(
        Guid actorUserId,
        Guid webhookId,
        UpdateWorkspaceWebhookRequest request,
        CancellationToken cancellationToken = default);

    Task<int> DeleteWebhookAsync(Guid actorUserId, Guid webhookId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<WebhookDeliveryLogDto>?> GetDeliveriesPageAsync(
        Guid actorUserId,
        Guid webhookId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, object? Body)> TestWebhookAsync(
        Guid actorUserId,
        Guid webhookId,
        CancellationToken cancellationToken = default);
}
