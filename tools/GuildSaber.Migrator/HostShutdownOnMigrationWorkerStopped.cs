using GuildSaber.Migrator.DiscordBot;

namespace GuildSaber.Migrator;

public class HostShutdownOnMigrationWorkerStopped(
    IHostApplicationLifetime hostApplicationLifetime
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while ((!Worker.IsFinished || !Server.Worker.IsFinished) && !stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);

        hostApplicationLifetime.StopApplication();
    }
}