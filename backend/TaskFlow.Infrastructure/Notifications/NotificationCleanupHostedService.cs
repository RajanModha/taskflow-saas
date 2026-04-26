using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Notifications;

public sealed class NotificationCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationCleanupHostedService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                var nextRunUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                var delay = nextRunUtc - now;
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.Zero;
                }

                await Task.Delay(delay, stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
            var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-90);

            var deleted = await db.Notifications
                .IgnoreQueryFilters()
                .Where(n => n.CreatedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                logger.LogInformation("Deleted {Count} expired notifications", deleted);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification cleanup job failed");
        }
    }
}
