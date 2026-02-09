using System.Diagnostics.CodeAnalysis;
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

    private readonly record struct PlayerWithGuilds(PlayerId PlayerId, GuildId[] GuildIds);

    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    private async Task DoWorkAsync() => await taskQueue.QueueBackgroundWorkItemAsync(async token =>
    {
        using var scope = scopeFactory.CreateScope();

        PlayerWithGuilds[] playersWithGuilds;
        await using (var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>())
        {
            playersWithGuilds = await dbContext.Players
                .Select(p => new PlayerWithGuilds(p.Id, p.Members.Select(x => x.GuildId).ToArray()))
                .ToArrayAsync(token);
        }

        var playerScoresPipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();
        var importAdminConfPipeline = scope.ServiceProvider.GetRequiredService<LegacyGSImportAdminConfPipeline>();

        foreach (var playerWithGuilds in playersWithGuilds)
        {
            var importedAny = false;
            foreach (var guildId in playerWithGuilds.GuildIds)
                importedAny |= await importAdminConfPipeline.ExecuteAsync(guildId, playerWithGuilds.PlayerId, token);

            if (importedAny) await playerScoresPipeline.RecalculatePlayerScoresAsync(playerWithGuilds.PlayerId, token);
        }
    });
}