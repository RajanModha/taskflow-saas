using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Activity;
using TaskFlow.Infrastructure.Persistence;
using ActivityLogEntity = TaskFlow.Domain.Entities.ActivityLog;

namespace TaskFlow.Infrastructure.Activity;

public sealed class ActivityLogger(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ActivityLogger> logger) : IActivityLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public System.Threading.Tasks.Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid actorId,
        string actorName,
        Guid organizationId,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        string? metaJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions);

        _ = PersistAsync(entityType, entityId, action, actorId, actorName, organizationId, metaJson);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private async System.Threading.Tasks.Task PersistAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid actorId,
        string actorName,
        Guid organizationId,
        string? metaJson)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
            var entry = new ActivityLogEntity
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                ActorId = actorId,
                ActorName = actorName,
                OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime,
                Metadata = metaJson,
                OrganizationId = organizationId,
            };

            await db.ActivityLogs.AddAsync(entry, CancellationToken.None);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to persist activity log {Action} for {EntityType} {EntityId}",
                action,
                entityType,
                entityId);
        }
    }
}
