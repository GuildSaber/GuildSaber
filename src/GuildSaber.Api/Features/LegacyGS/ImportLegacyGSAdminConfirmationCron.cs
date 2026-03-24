using System.Diagnostics.CodeAnalysis;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;

namespace GuildSaber.Api.Features.LegacyGS;

public class ImportLegacyGSAdminConfirmationCron(
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportLegacyGSAdminConfirmationCron> logger)
{
    private readonly record struct PlayerIdWithGuildIds(PlayerId PlayerId, GuildId[] GuildIds);

    [TickerFunction("ImportLegacyGSAdminConfirmation", cronExpression: "0 0 7 * * *")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    public async Task DoWorkAsync() => await taskQueue.QueueBackgroundWorkItemAsync(async token =>
    {
        logger.LogInformation("Starting legacy GuildSaber admin confirmation import job.");
        using var scope = scopeFactory.CreateScope();

        PlayerIdWithGuildIds[] playersWithGuilds;
        await using (var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>())
        {
            playersWithGuilds = await dbContext.Players
                .Select(p => new PlayerIdWithGuildIds(p.Id, p.Members.Select(x => x.GuildId).ToArray()))
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

        logger.LogInformation("Finished legacy GuildSaber admin confirmation import job.");
    });
}
