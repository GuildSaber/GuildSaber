using System.Diagnostics.CodeAnalysis;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;

namespace GuildSaber.Api.Features.LegacyGS.Jobs;

public class ImportLegacyGuildSaberAdminConfirmationsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ImportLegacyGuildSaberAdminConfirmationsJob> logger)
{
    private readonly record struct PlayerIdWithGuildIds(PlayerId PlayerId, GuildId[] GuildIds);

    [TickerFunction("ImportLegacyGSAdminConfirmation", cronExpression: "0 0 10 * * *")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    public async Task DoWorkAsync(TickerFunctionContext context, CancellationToken token)
    {
        logger.LogInformation("Starting legacy GuildSaber admin confirmation import job.");

        PlayerIdWithGuildIds[] playerIdsWithGuilds;
        using (var _ = scopeFactory.CreateScope())
        {
            playerIdsWithGuilds = await _.ServiceProvider.GetRequiredService<ServerDbContext>().Players
                .Select(p => new PlayerIdWithGuildIds(p.Id, p.Members.Select(x => x.GuildId).ToArray()))
                .ToArrayAsync(token);
        }

        using var scope = scopeFactory.CreateScope();
        var playerScoresPipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();
        var importAdminConfPipeline = scope.ServiceProvider.GetRequiredService<LegacyGSImportAdminConfPipeline>();

        foreach (var playerIdWithGuilds in playerIdsWithGuilds)
        {
            var importedAny = false;
            foreach (var guildId in playerIdWithGuilds.GuildIds)
                importedAny |= await importAdminConfPipeline.ExecuteAsync(guildId, playerIdWithGuilds.PlayerId, token);

            if (importedAny)
                await playerScoresPipeline.RecalculatePlayerScoresAsync(playerIdWithGuilds.PlayerId, token);
        }

        logger.LogInformation("Finished legacy GuildSaber admin confirmation import job.");
    }
}