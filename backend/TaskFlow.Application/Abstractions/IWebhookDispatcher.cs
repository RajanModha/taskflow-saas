namespace TaskFlow.Application.Abstractions;

public interface IWebhookDispatcher
{
    Task DispatchOrganizationEventAsync(
        Guid organizationId,
        string eventType,
        object data,
        CancellationToken cancellationToken = default);

    Task DispatchDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default);

    Task<(bool Delivered, int? ResponseStatus)> SendTestAsync(
        Guid organizationId,
        Guid webhookId,
        CancellationToken cancellationToken = default);
}
