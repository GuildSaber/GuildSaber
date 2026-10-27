using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedMaps.Pipelines;

/// <summary>Refreshes scores and member stats after a proposal's database transaction has committed.</summary>
public sealed class ApplyMapProposalPipeline(
    ServerDbContext dbContext,
    AddRankedMapPipeline addRankedMapPipeline,
    EditRankedMapPipeline editRankedMapPipeline,
    MemberPointStatsPipeline pointStatsPipeline,
    MemberAchievementStatsPipeline achievementStatsPipeline)
{
    public async Task ExecuteAsync(
        RankedMapId rankedMapId, SongDifficultyId[] addedDifficultyIds, CancellationToken token)
    {
        var context = await dbContext.RankedMaps.Where(x => x.Id == rankedMapId)
            .Include(x => x.Context.Points)
            .Select(x => x.Context)
            .SingleOrDefaultAsync(token);
        if (context is null) return;

        // Another proposal may have been accepted while this work waited in the queue.
        var addedDifficulties = await dbContext.SongDifficulties
            .Where(x => ((IEnumerable<SongDifficultyId>)addedDifficultyIds).Contains(x.Id)
                        && dbContext.MapVersions.Any(version
                            => version.RankedMapId == rankedMapId && version.SongDifficultyId == x.Id))
            .ToArrayAsync(token);

        foreach (var difficulty in addedDifficulties)
            await addRankedMapPipeline.ExecuteAsync(difficulty, token);

        await editRankedMapPipeline.ExecuteAsync(rankedMapId, token);

        // Removed versions and changed achievement/category membership can affect players without a remaining score.
        var players = await dbContext.ContextMembers
            .Where(x => x.ContextId == context.Id)
            .Select(x => x.PlayerId)
            .ToArrayAsync(token);

        foreach (var playerId in players)
        {
            token.ThrowIfCancellationRequested();
            await pointStatsPipeline.ExecuteAsync(playerId, context);

            foreach (var point in context.Points)
                await achievementStatsPipeline.ExecuteAsync(playerId, context.GuildId, context.Id, point.Id);

            dbContext.ChangeTracker.Clear();
        }
    }
}