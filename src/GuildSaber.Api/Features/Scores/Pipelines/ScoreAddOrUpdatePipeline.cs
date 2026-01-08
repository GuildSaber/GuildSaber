using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using EState = GuildSaber.Database.Models.Server.RankedScores.RankedScore.EState;
using EDenyReason = GuildSaber.Database.Models.Server.RankedScores.RankedScore.EDenyReason;

namespace GuildSaber.Api.Features.Scores.Pipelines;

public sealed class ScoreAddOrUpdatePipeline(
    ServerDbContext dbContext,
    MemberPointStatsPipeline memberPointStatsPipeline,
    IServiceScopeFactory scopeFactory,
    HybridCache cache)
{
    private static readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5)
    };

    private static readonly Func<ServerDbContext, PlayerId, IAsyncEnumerable<Context>>
        _getContextWithPointsForPlayerQuery = EF.CompileAsyncQuery((ServerDbContext dbContext, PlayerId playerId)
            => dbContext.ContextMembers
                .Where(x => x.PlayerId == playerId)
                .Include(x => x.Context).ThenInclude(c => c.Points)
                .Select(x => x.Context));

    /// <summary>
    /// Check if there is the same BeatLeader score already existing, ignoring the BeatLeaderScoreId.
    /// </summary>
    private static readonly Func<ServerDbContext, PlayerId, SongDifficultyId, DateTimeOffset, BaseScore,
        Task<BeatLeaderScore?>> _findExistingBeatLeaderScoreIgnoringBLScoreIdQuery = EF.CompileAsyncQuery((
            ServerDbContext dbContext, PlayerId playerId,
            SongDifficultyId songDifficultyId,
            DateTimeOffset setAt, BaseScore baseScore)
        => dbContext.BeatLeaderScores.FirstOrDefault(x =>
            x.PlayerId == playerId
            && x.SongDifficultyId == songDifficultyId
            && x.SetAt - setAt < TimeSpan.FromSeconds(30)
            && x.BaseScore == baseScore));

    private static readonly Func<ServerDbContext, PlayerId, SongDifficultyId, DateTimeOffset, BaseScore,
        Task<ScoreSaberScore?>> _findExistingScoreSaberScoreQuery = EF.CompileAsyncQuery((
            ServerDbContext dbContext, PlayerId playerId,
            SongDifficultyId songDifficultyId,
            DateTimeOffset setAt, BaseScore baseScore)
        => dbContext.ScoreSaberScores.FirstOrDefault(x =>
            x.PlayerId == playerId
            && x.SongDifficultyId == songDifficultyId
            && x.SetAt - setAt < TimeSpan.FromSeconds(30)
            && x.BaseScore == baseScore));

    private record ScoreRankingContext(
        RankedMap[] RankedMapsWithVersionsWithSongDifficulty,
        RankedScore[] ExistingRankedScores,
        Context[] ContextsWithPoints,
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
        AbstractScore Score
    );

    public readonly record struct PipelineResult(Context[] ImpactedContextsWithPoints);

    /// <remarks>
    /// There is codebase assumption that all scores going in are stored and updated by default, don't change this behavior.
    /// </remarks>
    public async Task<PipelineResult> ExecuteAsync(AbstractScore scoreToAdd)
        => await (await UpdateScoreIfChangedAsync(scoreToAdd, dbContext)
                .Or(() => dbContext.AddAndSaveAsync(scoreToAdd))
                .ToResult("Failed to add or update score.")
                .Map(async static (score, state) =>
                        await PrepareScoreRankingContextAsync(
                            score.PlayerId, score.SongDifficultyId, state.dbContext, state.scopeFactory, state.cache),
                    (dbContext, scopeFactory, cache))
                .Map(rankingContext => (rankingContext,
                    IterateRankedScoresWithTransform(rankingContext, RecalculateRankedScore)))
                .Map(tuple => (tuple.rankingContext, rankedScores: SetStateForBestRankedScorePerGroup(tuple.Item2))))
            .Map(static async (tuple, state) =>
            {
                var enumerated = tuple.rankedScores.ToArray();
                state.dbContext.RankedScores.UpdateRange(enumerated);
                await state.dbContext.SaveChangesAsync();

                /* An optimization at the cost of memory consumption would be:
                 * track the RankedScores (in EF Core with .AsTracking()),
                 * then only update the ranks for the RankedMaps that is tracked as changed. */
                var changedRankedMapIds = enumerated
                    .Select(x => x.RankedMapId)
                    .Distinct().ToArray();
                await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync(
                    changedRankedMapIds,
                    state.dbContext
                );

                // The same "ranked scores" might be used multiple time by the same context, we need to cleanup tracking.
                state.dbContext.ChangeTracker.Clear();
                return new PipelineResult(tuple.rankingContext.ContextsWithPoints);
            }, (dbContext, scoreToAdd.PlayerId, memberStatPipeline: memberPointStatsPipeline))
            .Unwrap();

    /// <summary>
    /// There is cases when Scores are processed and sent again from BeatLeader but with a score ID.
    /// In that case we want to update the existing score with the new ID if it doesn't already have one.
    /// There is also cases when the score is submitted multiple times, we don't want to create duplicates.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="dbContext">
    /// The database context to use for the operation.
    /// </param>
    private static async Task<Maybe<AbstractScore>> UpdateScoreIfChangedAsync(
        AbstractScore score, ServerDbContext dbContext)
        => score switch
        {
            BeatLeaderScore leaderScore => await UpdateScoreIfChangedAsync(leaderScore, dbContext),
            ScoreSaberScore saberScore => await UpdateScoreIfChangedAsync(saberScore, dbContext),
            _ => throw new InvalidOperationException("Score must be either a BeatLeaderScore or a ScoreSaberScore.")
        };

    /// <summary>
    /// Update the BeatLeader score if it already exists without an ID.
    /// If it doesn't exist, return None.
    /// If it exists and has no ID, just return the old score.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="dbContext"></param>
    /// <remarks>
    /// BeatLeader can send the same score multiple times, first without a BLScoreId, then with it.
    /// We want to avoid creating duplicates (when sent multiple times),
    /// and we also want to update the existing score with the BLScoreId when it arrives.
    /// </remarks>
    /// <returns></returns>
    private static async Task<Maybe<AbstractScore>> UpdateScoreIfChangedAsync(
        BeatLeaderScore score, ServerDbContext dbContext)
    {
        var oldScore = await _findExistingBeatLeaderScoreIgnoringBLScoreIdQuery(
            dbContext,
            score.PlayerId,
            score.SongDifficultyId,
            score.SetAt,
            score.BaseScore);
        if (oldScore is null)
            return None;

        /* It was already there, but the incoming score has no scoreId, there is no need to update the score.
         * It might just be a full score recalculation/refresh (no need to update). */
        if (score.BeatLeaderScoreId is null)
            return oldScore;

        score.Id = oldScore.Id;
        dbContext.BeatLeaderScores.Update(score);
        await dbContext.SaveChangesAsync();

        return score;
    }

    private static async Task<Maybe<AbstractScore>> UpdateScoreIfChangedAsync(
        ScoreSaberScore score, ServerDbContext dbContext)
    {
        var oldScore = await _findExistingScoreSaberScoreQuery(
            dbContext,
            score.PlayerId,
            score.SongDifficultyId,
            score.SetAt,
            score.BaseScore);
        if (oldScore is null)
            return None;

        score.Id = oldScore.Id;
        dbContext.ScoreSaberScores.Update(score);
        await dbContext.SaveChangesAsync();

        return score;
    }

    private static async Task<ScoreRankingContext> PrepareScoreRankingContextAsync(
        PlayerId playerId, SongDifficultyId songDifficultyId, ServerDbContext dbContext,
        IServiceScopeFactory scopeFactory, HybridCache cache)
    {
        var contextsWithPointsForPlayer = await GetContextWithPointsForPlayerAsync(playerId, cache, scopeFactory);
        var contextIdsForPlayer = contextsWithPointsForPlayer
            .Select(x => x.Id)
            .ToArray() as IEnumerable<ContextId>;

        var rankedMaps = await dbContext.RankedMaps
            .Include(x => x.MapVersions).ThenInclude(x => x.SongDifficulty)
            .Where(x => contextIdsForPlayer.Contains(x.ContextId)
                        && x.MapVersions.Any(v => v.SongDifficultyId == songDifficultyId))
            .ToArrayAsync();
        var rankedMapsIds = rankedMaps
            .Select(x => x.Id)
            .ToArray() as IEnumerable<RankedMap.RankedMapId>;
        var rankedScores = await dbContext.RankedScores
            .Where(x => x.PlayerId == playerId && rankedMapsIds.Contains(x.RankedMapId))
            .ToArrayAsync();

        var mapVersions = rankedMaps.SelectMany(x => x.MapVersions).ToArray();
        var songDifficultyIds = mapVersions.Select(x => x.SongDifficultyId).ToArray() as IEnumerable<SongDifficultyId>;

        // We grab all the scores that are related to the Ranked maps, not just the ranked map version.
        var scores = await dbContext.Scores
            .Where(x => x.PlayerId == playerId && songDifficultyIds.Contains(songDifficultyId))
            .ToArrayAsync();

        return new ScoreRankingContext(
            rankedMaps, rankedScores, contextsWithPointsForPlayer, scores
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
    /// Processes ranked scores by grouping them by RankedMapId and PointId.
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
    internal static IEnumerable<RankedScore> SetStateForBestRankedScorePerGroup(IEnumerable<RankedScore> rankedScores)
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

    private static ValueTask<Context[]> GetContextWithPointsForPlayerAsync(
        PlayerId playerId, HybridCache cache, IServiceScopeFactory scopeFactory)
        => cache.GetOrCreateAsync($"ContextWithPointsForPlayer_{playerId}", (scopeFactory, playerId),
            async static (state, token) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await _getContextWithPointsForPlayerQuery(dbContext, state.playerId)
                    .ToArrayAsync(token);
            }, _cacheEntryOptions);
}