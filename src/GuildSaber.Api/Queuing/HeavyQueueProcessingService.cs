namespace GuildSaber.Api.Queuing;

public class HeavyQueueProcessingService(
    IHeavyBackgroundTaskQueue taskQueue,
    ILogger<HeavyQueueProcessingService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{HeavyQueueProcessingService} is running.\n\n", nameof(HeavyQueueProcessingService));
        return ProcessTaskQueueAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await taskQueue.DequeueAsync(stoppingToken);
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error occurred executing task work item.");
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{HeavyQueueProcessingService} is stopping.", nameof(HeavyQueueProcessingService));
        await base.StopAsync(stoppingToken);
    }
}