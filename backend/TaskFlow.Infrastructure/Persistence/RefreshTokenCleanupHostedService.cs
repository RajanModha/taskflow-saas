using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>Removes refresh token rows that expired more than 90 days ago. Runs weekly.</summary>
public sealed class RefreshTokenCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunCleanupOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Initial refresh token cleanup failed.");
        }

        using var timer = new PeriodicTimer(TimeSpan.FromDays(7));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RunCleanupOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Scheduled refresh token cleanup failed.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    private async Task RunCleanupOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var deleted = await db.RefreshTokens
            .Where(t => t.ExpiresAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation("Refresh token cleanup removed {Count} expired rows (ExpiresAtUtc before {Cutoff}).", deleted, cutoff);
        }
    }
}
