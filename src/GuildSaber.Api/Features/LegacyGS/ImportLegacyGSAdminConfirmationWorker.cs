using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.LegacyGS;

public class ImportLegacyGSAdminConfirmationWorker(
    PeriodicTimer period,
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportLegacyGSAdminConfirmationWorker> logger) : BackgroundService
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

        var playerScoresPipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();
        var importAdminConfPipeline = scope.ServiceProvider.GetRequiredService<LegacyGSImportAdminConfPipeline>();

        foreach (var playerId in playerIds)
        {
            var importedAny = await importAdminConfPipeline.ExecuteAsync(playerId, token);
            if (importedAny) await playerScoresPipeline.RecalculatePlayerScoresAsync(playerId, token);
        }
    });
}