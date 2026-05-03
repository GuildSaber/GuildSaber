using System.Diagnostics.CodeAnalysis;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;

namespace GuildSaber.Api.Features.LegacyGS;

public class ImportLegacyGSMapImportCron(
    IServiceScopeFactory scopeFactory,
    ILogger<ImportLegacyGSMapImportCron> logger)
{
    private readonly record struct GuildIdWithContextIds(GuildId GuildId, ContextId[] ContextIds);

    [TickerFunction("ImportLegacyGSMap", cronExpression: "0 0 5 * * *")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    public async Task DoWorkAsync(CancellationToken token)
    {
        logger.LogInformation("Starting legacy GuildSaber map import job.");

        GuildIdWithContextIds[] guildIdsWithContextIds;
        using (var _ = scopeFactory.CreateScope())
        {
            guildIdsWithContextIds = await _.ServiceProvider.GetRequiredService<ServerDbContext>().Guilds
                .Select(x => new GuildIdWithContextIds(x.Id, x.Contexts.Select(y => y.Id).ToArray()))
                .ToArrayAsync(token);
        }

        foreach (var guildWithContextIds in guildIdsWithContextIds)
        foreach (var contextId in guildWithContextIds.ContextIds)
        {
            // Creating a different scope for each execution to isolate clearly the DbContext instances.
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LegacyGuildSaberMapImportPipeline>()
                .ExecuteAsync(guildWithContextIds.GuildId, contextId, token);
        }

        logger.LogInformation("Finished legacy GuildSaber map import job.");
    }
}