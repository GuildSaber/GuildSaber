using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;
using EState = GuildSaber.Database.Models.Server.RankedScores.RankedScore.EState;
using EDenyReason = GuildSaber.Database.Models.Server.RankedScores.RankedScore.EDenyReason;

namespace GuildSaber.Api.Features.Scores.Pipelines;

public sealed class ScoreAddOrUpdatePipeline(ServerDbContext dbContext)
{
    private record ScoreRankingContext(
        RankedMap[] RankedMapsWithVersionsWithSongDifficulty,
        RankedScore[] ExistingRankedScores,
        GuildContext[] ContextsWithPoints,
        AbstractScore[] Scores
    );

    /// <summary>
    /// Represents a processing context for a single score within the ranking system calculation.
    /// </summary>
    /// <remarks>
    /// This context encapsulates all components needed to evaluate a single score:
    /// <list type="bullet">
    ///     <item>The ranked map that contains the evaluated song difficulty</item>
    ///     <item>The specific song difficulty (tied to a map version being processed)</item>
    ///     <item>The point configuration from a guild context that defines scoring rules</item>
    ///     <item>The player's actual score for this song difficulty</item>
    /// </list>
    /// Used when iterating through all combinations of map versions and points to calculate
    /// individual ranked scores before selecting the best ones.
    /// </remarks>
    private record RankedScoreTransformContext(
        RankedMap Map,
        SongDifficulty SongDifficulty,
        Point Point,
        AbstractScore Score);

    public async Task ExecuteAsync(AbstractScore scoreToAdd)
        => await (await UpdateScoreIfChangedAsync(scoreToAdd, dbContext)
                .Or(() => dbContext.AddAndSaveAsync(scoreToAdd))
                .ToResult("Failed to add or update score.")
                .Map(async static (score, dbContext) =>
                    await PrepareScoreRankingContextAsync(score.PlayerId, score.SongDifficultyId, dbContext), dbContext)
                .Map(x => IterateRankedScoresWithTransform(x, RecalculateRankedScore))
                .Map(FilterAndSelectScoresByGroup))
            .Map(static async (rankedScores, state) =>
            {
                var enumerable = rankedScores.ToArray();
                state.dbContext.RankedScores.UpdateRange(enumerable);
                await state.dbContext.SaveChangesAsync();

                /* An optimization at the cost of memory consumption would be:
                 * track the RankedScores (in EF Core with .AsTracking()),
                 * then only update the ranks for the RankedMaps that is tracked as changed. */
                var changedRankedMapIds = enumerable
                    .Select(x => x.RankedMapId)
                    .Distinct().ToArray();
                await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync(
                    changedRankedMapIds,
                    state.dbContext
                );

                await PlayerPointsPipeline.RecalculatePlayerPoints(state.PlayerId, state.dbContext);
                await PlayerLevelPipeline.RecalculatePlayerLevels(state.PlayerId, state.dbContext);
            }, (dbContext, scoreToAdd.PlayerId))
            .Unwrap();

    /// <summary>
    /// There is cases when Scores are processed and sent again from BeatLeader but with a score ID.
    /// In that case we want to update the existing score with the new ID if it doesn't already have one.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="dbContext">
    /// The database context to use for the operation.
    /// </param>
    private static async Task<Maybe<AbstractScore>> UpdateScoreIfChangedAsync(
        AbstractScore score, ServerDbContext dbContext)
    {
        if (score is not BeatLeaderScore { BeatLeaderScoreId: not null } blScore)
            return None;

        var oldScore = await SameBeatLeaderScoreWithoutIdExistsAsync(blScore, dbContext);
        if (oldScore is null)
            return None;

        blScore.Id = oldScore.Id;
        dbContext.BeatLeaderScores.Update(blScore);
        await dbContext.SaveChangesAsync();

        return blScore;
    }

    /// <summary>
    /// Check if there is the same BeatLeader score (without an ID) already existing in the database.
    /// </summary>
    private static Task<BeatLeaderScore?> SameBeatLeaderScoreWithoutIdExistsAsync(
        BeatLeaderScore score, ServerDbContext dbContext)
        => dbContext.BeatLeaderScores.FirstOrDefaultAsync(x =>
            x.PlayerId == score.PlayerId
            && x.SongDifficultyId == score.SongDifficultyId
            && x.SetAt - score.SetAt < TimeSpan.FromSeconds(30)
            && x.BaseScore == score.BaseScore);

    private static async Task<ScoreRankingContext> PrepareScoreRankingContextAsync(
        PlayerId playerId, SongDifficultyId songDifficultyId, ServerDbContext dbContext)
    {
        var rankedMaps = await dbContext.RankedMaps
            .Include(x => x.MapVersions).ThenInclude(x => x.SongDifficulty)
            .Where(x => x.MapVersions.Any(v => v.SongDifficultyId == songDifficultyId))
            .ToArrayAsync();
        var rankedMapsIds = rankedMaps.Select(x => x.Id).ToArray();
        var rankedScores = await dbContext.RankedScores
            .Where(x => x.PlayerId == playerId && rankedMapsIds.Contains(x.RankedMapId))
            .ToArrayAsync();
        var contextWithPoints = await dbContext.GuildContexts
            .Include(x => x.Points)
            .Where(x => x.RankedMaps.Any(y => y.ContextId == x.Id))
            .ToArrayAsync();

        var mapVersions = rankedMaps.SelectMany(x => x.MapVersions).ToArray();
        var songDifficultyIds = mapVersions.Select(x => x.SongDifficultyId).ToArray();

        // We grab all the scores that are related to the Ranked maps, not just the ranked map version.
        var scores = await dbContext.Scores
            .Where(x => x.PlayerId == playerId && songDifficultyIds
                .Contains(songDifficultyId))
            .ToArrayAsync();

        return new ScoreRankingContext(
            rankedMaps, rankedScores, contextWithPoints, scores
        );
    }

    private static IEnumerable<RankedScore> IterateRankedScoresWithTransform(
        ScoreRankingContext rankingContext,
        Func<RankedScoreTransformContext, RankedScore, RankedScore> transform)
    {
        foreach (var rankedMap in rankingContext.RankedMapsWithVersionsWithSongDifficulty)
        foreach (var mapVersion in rankedMap.MapVersions)
        foreach (var point in rankingContext.ContextsWithPoints.First(x => x.Id == rankedMap.ContextId).Points)
        foreach (var score in rankingContext.Scores.Where(x => x.SongDifficultyId == mapVersion.SongDifficultyId))
        {
            var transformContext = new RankedScoreTransformContext(
                rankedMap,
                mapVersion.SongDifficulty,
                point,
                score
            );

            var rankedScores = rankingContext.ExistingRankedScores.Where(y =>
                y.ScoreId == score.Id
                && y.RankedMapId == rankedMap.Id
                && y.SongDifficultyId == mapVersion.SongDifficultyId
                && y.ContextId == rankedMap.ContextId
                && y.PointId == point.Id
            );

            var anyRankedScores = false;
            foreach (var rankedScore in rankedScores)
            {
                anyRankedScores = true;
                yield return transform(transformContext, rankedScore);
            }

            if (!anyRankedScores)
                yield return transform(transformContext, new RankedScore
                {
                    GuildId = rankedMap.GuildId,
                    ContextId = rankedMap.ContextId,
                    RankedMapId = mapVersion.RankedMapId,
                    SongDifficultyId = mapVersion.SongDifficultyId,
                    PointId = point.Id,
                    PlayerId = score.PlayerId,
                    ScoreId = score.Id,
                    PrevScoreId = null,
                    State = EState.None,
                    DenyReason = EDenyReason.Unspecified,
                    EffectiveScore = default,
                    RawPoints = default,
                    Rank = 0
                });
        }
    }

    private static RankedScore RecalculateRankedScore(RankedScoreTransformContext context, RankedScore rankedScore)
    {
        rankedScore.EffectiveScore = ScoringUtils.CalculateScoreFromModifiers(
            context.Score.BaseScore,
            context.Score.Modifiers,
            context.Point.ModifierValues
        );

        (rankedScore.State, rankedScore.DenyReason) = ScoringUtils.RecalculateStateAndReason(
            rankedScore.State,
            context.Score,
            context.Map.Requirements,
            context.SongDifficulty.Stats
        );

        rankedScore.RawPoints = ScoringUtils.CalculateRawPoints(
            context.Score.BaseScore,
            rankedScore.EffectiveScore,
            context.SongDifficulty.Stats.MaxScore,
            context.Point,
            context.Map.Rating
        );

        return rankedScore;
    }

    /// <summary>
    /// Filters and processes ranked scores by grouping them by RankedMapId and PointId.
    /// For each group, this method:
    /// <list type="bullet">
    ///     <item>
    ///         <description>Finds the highest-scoring entry (using default comparison)</description>
    ///     </item>
    ///     <item>
    ///         <description>Removes the Selected state from all persisted scores</description>
    ///     </item>
    ///     <item>
    ///         <description>Sets only the best score to Selected state</description>
    ///     </item>
    /// </list>
    /// This ensures that for each (RankedMapId, PointId) combination, only one score is marked as Selected.
    /// </summary>
    /// <param name="rankedScores">Collection of ranked scores to process</param>
    /// <returns>Processed collection of ranked scores with appropriate Selected state</returns>
    internal static IEnumerable<RankedScore> FilterAndSelectScoresByGroup(IEnumerable<RankedScore> rankedScores)
        => rankedScores
            .GroupBy(x => (x.RankedMapId, x.PointId))
            .SelectMany(group => group.Max() switch
            {
                null => throw new InvalidOperationException("Group should contain at least one element."),
                var best => group.Select(x => x == best ? AddSelectedState(x) : RemoveSelectedState(x))
            });

    private static RankedScore RemoveSelectedState(RankedScore rankedScore)
    {
        rankedScore.State &= ~EState.Selected;
        return rankedScore;
    }

    private static RankedScore AddSelectedState(RankedScore rankedScore)
    {
        rankedScore.State |= EState.Selected;
        return rankedScore;
    }
}