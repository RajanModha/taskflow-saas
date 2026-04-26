using System.Net.Http.Headers;
using Task = System.Threading.Tasks.Task;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Webhooks;

public sealed class WebhookDispatcher(
    TaskFlowDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    WebhookSecretProtector secretProtector,
    TimeProvider timeProvider,
    ILogger<WebhookDispatcher> logger) : IWebhookDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task DispatchOrganizationEventAsync(
        Guid organizationId,
        string eventType,
        object data,
        CancellationToken cancellationToken = default)
    {
        var webhooks = await dbContext.Webhooks
            .IgnoreQueryFilters()
            .Where(w => w.OrganizationId == organizationId && w.IsActive)
            .ToListAsync(cancellationToken);

        var targets = webhooks
            .Where(w => WebhookJson.EventListContains(w.Events, eventType))
            .ToList();
        if (targets.Count == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var occurredAt = timeProvider.GetUtcNow();
        foreach (var wh in targets)
        {
            var deliveryId = Guid.NewGuid();
            var envelope = new WebhookEnvelope(deliveryId, eventType, occurredAt, organizationId, data);
            var json = JsonSerializer.Serialize(envelope, JsonOptions);
            var delivery = new WebhookDelivery
            {
                Id = deliveryId,
                WebhookId = wh.Id,
                EventType = eventType,
                Payload = json,
                Status = WebhookDeliveryStatuses.Pending,
                AttemptCount = 0,
                CreatedAtUtc = now,
            };
            await dbContext.WebhookDeliveries.AddAsync(delivery, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DispatchDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await dbContext.WebhookDeliveries
            .IgnoreQueryFilters()
            .Include(d => d.Webhook)
            .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken);

        if (delivery is null || delivery.Webhook is null)
        {
            return;
        }

        if (delivery.Status != WebhookDeliveryStatuses.Pending)
        {
            return;
        }

        if (!delivery.Webhook.IsActive)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var eligible = delivery.LastAttemptAt is null
            || (delivery.NextRetryAt is not null && delivery.NextRetryAt <= now);
        if (!eligible)
        {
            return;
        }

        if (WebhookUrlValidator.Validate(delivery.Webhook.Url) is { } urlError)
        {
            delivery.Status = WebhookDeliveryStatuses.Failed;
            delivery.LastAttemptAt = timeProvider.GetUtcNow().UtcDateTime;
            delivery.ResponseBody = urlError;
            delivery.ResponseStatus = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        delivery.AttemptCount += 1;
        delivery.LastAttemptAt = now;
        delivery.NextRetryAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var jsonPayload = delivery.Payload;
        string plainSecret;
        try
        {
            plainSecret = secretProtector.Unprotect(delivery.Webhook.Secret);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook {WebhookId} secret could not be unprotected; marking delivery failed.", delivery.WebhookId);
            delivery.Status = WebhookDeliveryStatuses.Failed;
            delivery.ResponseStatus = null;
            delivery.ResponseBody = "Secret could not be read.";
            delivery.Webhook.IsActive = false;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var sig = Convert.ToBase64String(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(plainSecret), Encoding.UTF8.GetBytes(jsonPayload)));
        var signatureHeader = $"sha256={sig}";

        var client = httpClientFactory.CreateClient("Webhooks");
        using var request = new HttpRequestMessage(HttpMethod.Post, delivery.Webhook.Url);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("X-TaskFlow-Signature", signatureHeader);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await ReadResponseBodyAsync(response, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                delivery.Status = WebhookDeliveryStatuses.Success;
                delivery.ResponseStatus = (int)response.StatusCode;
                delivery.ResponseBody = body;
                delivery.NextRetryAt = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            await RecordFailureAsync(delivery, response, (int)response.StatusCode, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook delivery {DeliveryId} HTTP request failed.", deliveryId);
            await RecordFailureAsync(delivery, null, null, ex.Message, cancellationToken);
        }
    }

    public async Task<(bool Delivered, int? ResponseStatus)> SendTestAsync(
        Guid organizationId,
        Guid webhookId,
        CancellationToken cancellationToken = default)
    {
        var webhook = await dbContext.Webhooks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.Id == webhookId && w.OrganizationId == organizationId,
                cancellationToken);

        if (webhook is null || !webhook.IsActive)
        {
            return (false, null);
        }

        if (WebhookUrlValidator.Validate(webhook.Url) is not null)
        {
            return (false, null);
        }

        var occurredAt = timeProvider.GetUtcNow();
        var envelope = new WebhookEnvelope(
            Guid.NewGuid(),
            WebhookEventTypes.WebhookTest,
            occurredAt,
            organizationId,
            new { message = "Test delivery" });
        var json = JsonSerializer.Serialize(envelope, JsonOptions);

        string plainSecret;
        try
        {
            plainSecret = secretProtector.Unprotect(webhook.Secret);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook {WebhookId} test: secret unreadable.", webhookId);
            return (false, null);
        }

        var sig = Convert.ToBase64String(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(plainSecret), Encoding.UTF8.GetBytes(json)));
        var client = httpClientFactory.CreateClient("Webhooks");
        using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
        request.Content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("X-TaskFlow-Signature", $"sha256={sig}");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var delivered = response.IsSuccessStatusCode;
            return (delivered, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook {WebhookId} test POST failed.", webhookId);
            return (false, null);
        }
    }

    private async Task RecordFailureAsync(
        WebhookDelivery delivery,
        HttpResponseMessage? response,
        int? statusCode,
        string? bodyOrError,
        CancellationToken cancellationToken)
    {
        delivery.ResponseStatus = statusCode ?? (response is not null ? (int)response.StatusCode : null);
        delivery.ResponseBody = Truncate(bodyOrError, 8000);

        if (delivery.AttemptCount >= 5)
        {
            delivery.Status = WebhookDeliveryStatuses.Failed;
            delivery.NextRetryAt = null;
            if (delivery.Webhook is not null)
            {
                delivery.Webhook.IsActive = false;
            }
        }
        else
        {
            var delayMinutes = Math.Pow(2, delivery.AttemptCount);
            delivery.NextRetryAt = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(delayMinutes);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string?> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return Truncate(text, 8000);
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? text, int max)
    {
        if (text is null)
        {
            return null;
        }

        return text.Length <= max ? text : text[..max];
    }
}
