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

public sealed class ScoreUpdatePipeline(ServerDbContext dbContext)
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

    public async Task AddOrUpdateAsync(AbstractScore scoreToAdd)
        => await UpdateScoreIfChangedAsync(scoreToAdd, dbContext).Or(() => dbContext.AddAndSaveAsync(scoreToAdd))
            .ToResult("Failed to add or update score.")
            .Map(static (score, dbContext) =>
                PrepareScoreRankingContextAsync(score.PlayerId, score.SongDifficultyId, dbContext), dbContext)
            .Map(x => IterateRankedScoresWithTransform(x, RecalculateRankedScore))
            .Map(FilterAndSelectScoresByGroup)
            .Map(static (rankedScores, state) => state.dbContext.Database.CreateExecutionStrategy()
                    .ExecuteInTransactionAsync((rankedScores, state.dbContext, state.PlayerId),
                        operation: static async (state, _) =>
                        {
                            await state.dbContext.BulkInsertOrUpdateAsync(state.rankedScores);
                            await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync(state.rankedScores
                                .Select(x => x.RankedMapId)
                                .Distinct(), state.dbContext);
                            await PlayerPointsPipeline.RecalculatePlayerPoints(state.PlayerId, state.dbContext);
                            await PlayerLevelPipeline.RecalculatePlayerLevels(state.PlayerId, state.dbContext);
                        }, verifySucceeded: (_, _) => Task.FromResult(true)),
                (dbContext, scoreToAdd.PlayerId))
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
        if (score is not BeatLeaderScore { BeatLeaderScoreId: not null } blScore
            || !await SameBeatLeaderScoreWithoutIdExistsAsync(blScore, dbContext))
            return None;

        dbContext.BeatLeaderScores.Update(blScore);
        await dbContext.SaveChangesAsync();

        return blScore;
    }

    /// <summary>
    /// Check if there is the same BeatLeader score (without an ID) already existing in the database.
    /// </summary>
    private static Task<bool> SameBeatLeaderScoreWithoutIdExistsAsync(BeatLeaderScore score, ServerDbContext dbContext)
        => dbContext.BeatLeaderScores.AnyAsync(x =>
            x.PlayerId == score.PlayerId
            && x.SongDifficultyId == score.SongDifficultyId
            && x.SetAt == score.SetAt && x.BaseScore == score.BaseScore
            && x.BeatLeaderScoreId == null);

    private static async Task<ScoreRankingContext> PrepareScoreRankingContextAsync(
        PlayerId playerId, SongDifficultyId songDifficultyId, ServerDbContext dbContext)
    {
        var rankedMaps = await dbContext.RankedMaps
            .Include(x => x.MapVersions).ThenInclude(x => x.SongDifficulty)
            .Where(x => x.MapVersions.Any(v => v.SongDifficultyId == songDifficultyId))
            .ToArrayAsync();
        var rankedScores = await dbContext.RankedScores
            .Where(x => x.PlayerId == playerId && rankedMaps.Any(y => y.Id == x.RankedMapId))
            .ToArrayAsync();
        var contextWithPoints = await dbContext.GuildContexts
            .Include(x => x.Points)
            .Where(x => x.RankedMaps.Any(y => y.ContextId == x.Id))
            .ToArrayAsync();

        var mapVersions = rankedMaps.SelectMany(x => x.MapVersions).ToArray();

        // We grab all the scores that are related to the Ranked maps, not just the ranked map version.
        var scores = await dbContext.Scores
            .Where(x => x.PlayerId == playerId && mapVersions
                .Any(y => y.SongDifficultyId == songDifficultyId))
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
        foreach (var guildContext in rankingContext.ContextsWithPoints)
        foreach (var point in guildContext.Points)
        foreach (var score in rankingContext.Scores)
        {
            var transformContext = new RankedScoreTransformContext(
                rankedMap,
                mapVersion.SongDifficulty,
                point,
                score
            );

            var rankedScore = rankingContext.ExistingRankedScores
                .FirstOrDefault(y => y.ScoreId == score.Id && y.ContextId == guildContext.Id && y.PointId == point.Id);

            rankedScore ??= new RankedScore
            {
                GuildId = guildContext.GuildId,
                ContextId = guildContext.Id,
                RankedMapId = mapVersion.RankedMapId,
                SongDifficultyId = mapVersion.SongDifficultyId,
                PointId = point.Id,
                PlayerId = score.PlayerId,
                ScoreId = score.Id,
                PrevScoreId = default,
                State = EState.None,
                DenyReason = EDenyReason.Unspecified,
                EffectiveScore = default,
                RawPoints = default,
                Rank = 0
            };

            yield return transform(transformContext, rankedScore);
        }
    }

    private static RankedScore RecalculateRankedScore(RankedScoreTransformContext context, RankedScore rankedScore)
    {
        rankedScore.EffectiveScore = ScoringUtils.CalculateScoreFromModifiers(
            context.Score.BaseScore,
            context.Score.Modifiers,
            context.Point.ModifierSettings
        );

        (rankedScore.State, rankedScore.DenyReason) = ScoringUtils.RecalculateStateAndReason(
            rankedScore.State,
            context.Score,
            context.Map.Requirements,
            context.SongDifficulty.Stats
        );

        rankedScore.RawPoints = ScoringUtils.CalculateRawPoints(
            rankedScore.EffectiveScore,
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
    ///         <description>Filters out scores that should not be persisted</description>
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
                var best => group
                    .Where(RankedScoresShouldBePersisted)
                    .Select(RemoveSelectedState)
                    .UnionBy([SetStateToSelected(best)], x => x.Id)
            });

    /// <summary>
    /// RankedScores that should be persisted are the stateful ones (Selected, Approved, Refused, Pending).
    /// </summary>
    private static bool RankedScoresShouldBePersisted(RankedScore rankedScore)
        => !rankedScore.State.HasFlag(EState.Denied);

    private static RankedScore RemoveSelectedState(RankedScore rankedScore)
    {
        rankedScore.State &= ~EState.Selected;
        return rankedScore;
    }

    private static RankedScore SetStateToSelected(RankedScore rankedScore)
    {
        rankedScore.State = EState.Selected;
        return rankedScore;
    }
}