using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Webhooks;

public sealed class WorkspaceWebhookService(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider,
    WebhookSecretProtector secretProtector,
    IWebhookDispatcher webhookDispatcher) : IWorkspaceWebhookService
{
    public async Task<IReadOnlyList<WebhookDto>?> ListWebhooksAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var rows = await dbContext.Webhooks
            .AsNoTracking()
            .Where(w => w.OrganizationId == actor.OrganizationId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows
            .Select(w => new WebhookDto(
                w.Id,
                w.Url,
                WebhookJson.DeserializeEventList(w.Events),
                w.IsActive,
                w.CreatedAtUtc))
            .ToList();
    }

    public async Task<(int StatusCode, object? Body)> CreateWebhookAsync(
        Guid actorUserId,
        CreateWorkspaceWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        var urlError = ValidateHttpsUrl(request.Url);
        if (urlError is not null)
        {
            return (StatusCodes.Status400BadRequest, new { message = urlError });
        }

        var eventsError = ValidateEventList(request.Events, requireAny: true);
        if (eventsError is not null)
        {
            return (StatusCodes.Status400BadRequest, new { message = eventsError });
        }

        var secret = request.Secret.Trim();
        if (secret.Length < 8 || secret.Length > 512)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Secret must be between 8 and 512 characters." });
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Url = request.Url.Trim(),
            Secret = secretProtector.Protect(secret),
            Events = WebhookJson.SerializeEventList(request.Events),
            IsActive = true,
            CreatedAtUtc = now,
            CreatedByUserId = actorUserId,
        };

        await dbContext.Webhooks.AddAsync(webhook, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new WebhookDto(
            webhook.Id,
            webhook.Url,
            WebhookJson.DeserializeEventList(webhook.Events),
            webhook.IsActive,
            webhook.CreatedAtUtc);

        return (StatusCodes.Status201Created, dto);
    }

    public async Task<(int StatusCode, object? Body)> UpdateWebhookAsync(
        Guid actorUserId,
        Guid webhookId,
        UpdateWorkspaceWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        if (request.Url is null && request.Events is null && request.IsActive is null && request.Secret is null)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Provide at least one field to update." });
        }

        var webhook = await dbContext.Webhooks
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.OrganizationId == actor.OrganizationId, cancellationToken);
        if (webhook is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Webhook not found." });
        }

        if (request.Url is not null)
        {
            var urlError = ValidateHttpsUrl(request.Url);
            if (urlError is not null)
            {
                return (StatusCodes.Status400BadRequest, new { message = urlError });
            }

            webhook.Url = request.Url.Trim();
        }

        if (request.Events is not null)
        {
            var eventsError = ValidateEventList(request.Events, requireAny: true);
            if (eventsError is not null)
            {
                return (StatusCodes.Status400BadRequest, new { message = eventsError });
            }

            webhook.Events = WebhookJson.SerializeEventList(request.Events);
        }

        if (request.IsActive is not null)
        {
            webhook.IsActive = request.IsActive.Value;
        }

        if (request.Secret is not null)
        {
            var secret = request.Secret.Trim();
            if (secret.Length < 8 || secret.Length > 512)
            {
                return (StatusCodes.Status400BadRequest, new { message = "Secret must be between 8 and 512 characters." });
            }

            webhook.Secret = secretProtector.Protect(secret);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new WebhookDto(
            webhook.Id,
            webhook.Url,
            WebhookJson.DeserializeEventList(webhook.Events),
            webhook.IsActive,
            webhook.CreatedAtUtc);

        return (StatusCodes.Status200OK, dto);
    }

    public async Task<int> DeleteWebhookAsync(Guid actorUserId, Guid webhookId, CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return StatusCodes.Status404NotFound;
        }

        var deleted = await dbContext.Webhooks
            .Where(w => w.Id == webhookId && w.OrganizationId == actor.OrganizationId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
    }

    public async Task<PagedResultDto<WebhookDeliveryLogDto>?> GetDeliveriesPageAsync(
        Guid actorUserId,
        Guid webhookId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var exists = await dbContext.Webhooks
            .AsNoTracking()
            .AnyAsync(w => w.Id == webhookId && w.OrganizationId == actor.OrganizationId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var query = dbContext.WebhookDeliveries
            .AsNoTracking()
            .Where(d => d.WebhookId == webhookId)
            .OrderByDescending(d => d.CreatedAtUtc);

        var total = await query.LongCountAsync(cancellationToken);
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(d => new WebhookDeliveryLogDto(
                d.Id,
                d.EventType,
                d.Status,
                d.AttemptCount,
                d.LastAttemptAt,
                d.ResponseStatus))
            .ToListAsync(cancellationToken);

        return PagedResultDto<WebhookDeliveryLogDto>.Create(items, safePage, safePageSize, total);
    }

    public async Task<(int StatusCode, object? Body)> TestWebhookAsync(
        Guid actorUserId,
        Guid webhookId,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        var exists = await dbContext.Webhooks
            .AsNoTracking()
            .AnyAsync(w => w.Id == webhookId && w.OrganizationId == actor.OrganizationId, cancellationToken);
        if (!exists)
        {
            return (StatusCodes.Status404NotFound, new { message = "Webhook not found." });
        }

        var (delivered, status) = await webhookDispatcher.SendTestAsync(actor.OrganizationId, webhookId, cancellationToken);
        return (StatusCodes.Status200OK, new WebhookTestResponse(delivered, status));
    }

    private static string? ValidateHttpsUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "Url is required.";
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return "Url must be a valid HTTPS URL.";
        }

        return null;
    }

    private static string? ValidateEventList(IReadOnlyList<string> events, bool requireAny)
    {
        if (requireAny && events.Count == 0)
        {
            return "At least one event is required.";
        }

        foreach (var e in events)
        {
            if (!WebhookEventTypes.IsSupported(e))
            {
                return $"Unsupported event type: {e}";
            }
        }

        return null;
    }

    private async Task<ApplicationUser?> LoadAdminActorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.OrganizationId == Guid.Empty)
        {
            return null;
        }

        if (user.OrganizationId != currentTenant.OrganizationId || !currentTenant.IsSet)
        {
            return null;
        }

        if (user.WorkspaceRole != WorkspaceRole.Owner && user.WorkspaceRole != WorkspaceRole.Admin)
        {
            return null;
        }

        return user;
    }
}
