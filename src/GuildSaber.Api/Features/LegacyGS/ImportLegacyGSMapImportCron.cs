using System.Diagnostics.CodeAnalysis;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;

namespace GuildSaber.Api.Features.LegacyGS;

public class ImportLegacyGSMapImportCron(
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<ImportLegacyGSMapImportCron> logger)
{
    private readonly record struct GuildIdWithContextIds(GuildId GuildId, ContextId[] ContextIds);

    [TickerFunction("ImportLegacyGSMap", cronExpression: "0 0 5 * * *")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    public async Task DoWorkAsync() => await taskQueue.QueueBackgroundWorkItemAsync(async token =>
    {
        logger.LogInformation("Starting legacy GuildSaber map import job.");

        using var scope = scopeFactory.CreateScope();
        GuildIdWithContextIds[] guildIdsWithContextIds;

        await using (var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>())
        {
            guildIdsWithContextIds = await dbContext.Guilds
                .Select(x => new GuildIdWithContextIds(x.Id, x.Contexts.Select(y => y.Id).ToArray()))
                .ToArrayAsync(token);
        }

        foreach (var guildWithContextIds in guildIdsWithContextIds)
        foreach (var contextId in guildWithContextIds.ContextIds)
            await scope.ServiceProvider.GetRequiredService<LegacyGuildSaberMapImportPipeline>()
                .ExecuteAsync(guildWithContextIds.GuildId, contextId, token);

        logger.LogInformation("Finished legacy GuildSaber map import job.");
    });
}