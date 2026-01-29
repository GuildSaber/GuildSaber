using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Scores;

public class ImportLegacyGuildSaberAdminConfirmationWorker(
    PeriodicTimer period,
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportLegacyGuildSaberAdminConfirmationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await period.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Starting legacy GuildSaber admin confirmation import job.");
            await DoWorkAsync();
            logger.LogInformation("Finished legacy GuildSaber admin confirmation import job.");
        }
    }

    private async Task DoWorkAsync() => await taskQueue.QueueBackgroundWorkItemAsync(async token =>
    {
        using var scope = scopeFactory.CreateScope();

        PlayerId[] playerIds;
        await using (var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>())
        {
            playerIds = await dbContext.Players
                .Select(p => p.Id)
                .ToArrayAsync(token);
        }

        var pipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();
        foreach (var playerId in playerIds)
            await pipeline.ImportLegacyGuildSaberAdminConfirmationAsync(playerId, token);
    });
}