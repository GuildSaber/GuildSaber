namespace GuildSaber.Api.Queuing;

public class QueueProcessingService(IBackgroundTaskQueue taskQueue, ILogger<QueueProcessingService> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{QueueProcessingService} is running.\n\n", nameof(QueueProcessingService));
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
        logger.LogInformation("{QueueProcessingService} is stopping.", nameof(QueueProcessingService));
        await base.StopAsync(stoppingToken);
    }
}