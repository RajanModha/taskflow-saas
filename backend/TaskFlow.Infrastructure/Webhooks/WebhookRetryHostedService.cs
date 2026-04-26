using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Webhooks;

public sealed class WebhookRetryHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookRetryHostedService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();
                var now = timeProvider.GetUtcNow().UtcDateTime;

                var staleInFlightCutoff = now.AddMinutes(-2);
                var dueIds = await db.WebhookDeliveries
                    .IgnoreQueryFilters()
                    .Where(d =>
                        d.Status == WebhookDeliveryStatuses.Pending &&
                        ((d.NextRetryAt != null && d.NextRetryAt <= now) ||
                         (d.AttemptCount == 0 && d.LastAttemptAt == null) ||
                         (d.LastAttemptAt != null &&
                          d.NextRetryAt == null &&
                          d.LastAttemptAt < staleInFlightCutoff)))
                    .OrderBy(d => d.NextRetryAt ?? DateTime.MinValue)
                    .Select(d => d.Id)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var id in dueIds)
                {
                    await dispatcher.DispatchDeliveryAsync(id, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Webhook retry batch failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
