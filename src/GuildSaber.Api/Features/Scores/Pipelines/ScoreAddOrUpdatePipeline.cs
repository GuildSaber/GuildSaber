using System.Diagnostics;
using CSharpFunctionalExtensions;
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
using EInvalidReason = GuildSaber.Database.Models.Server.RankedScores.InvalidRankedScore.EInvalidReason;

namespace GuildSaber.Api.Features.Scores.Pipelines;

public sealed class ScoreAddOrUpdatePipeline(
    ServerDbContext dbContext,
    IServiceScopeFactory scopeFactory,
    HybridCache cache)
{
    private const string ContextWithPointsForPlayerCacheKeyPrefix = "ContextWithPointsForPlayer_";

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
            && x.SetAt - setAt < TimeSpan.FromSeconds(2)
            && x.BaseScore == baseScore));

    private static readonly Func<ServerDbContext, PlayerId, SongDifficultyId, DateTimeOffset, BaseScore,
        Task<ScoreSaberScore?>> _findExistingScoreSaberScoreQuery = EF.CompileAsyncQuery((
            ServerDbContext dbContext, PlayerId playerId,
            SongDifficultyId songDifficultyId,
            DateTimeOffset setAt, BaseScore baseScore)
        => dbContext.ScoreSaberScores.FirstOrDefault(x =>
            x.PlayerId == playerId
            && x.SongDifficultyId == songDifficultyId
            && x.SetAt - setAt < TimeSpan.FromSeconds(2)
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

    public ValueTask ClearPlayerCacheAsync(PlayerId playerId)
        => cache.RemoveAsync($"{ContextWithPointsForPlayerCacheKeyPrefix}{playerId}");

    /// <remarks>
    /// There is codebase assumption that all scores going in are stored and updated by default, don't change this behavior.
    /// </remarks>
    public async Task<PipelineResult> ExecuteAsync(AbstractScore scoreToAdd, CancellationToken token)
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
                /* We need to nullify the Score property before updating the RankedScore,
                 * otherwise EF Core will try to insert/update the AbstractScore (which is not tracked) and fail. */
                foreach (var rankedScore in enumerated)
                    rankedScore.Score = null!;

                state.dbContext.RankedScores.UpdateRange(enumerated);
                await state.dbContext.SaveChangesAsync(state.token);

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

                // The same "ranked scores" might be used multiple time by the same context, we need to clean up tracking.
                state.dbContext.ChangeTracker.Clear();
                return new PipelineResult(tuple.rankingContext.ContextsWithPoints);
            }, (dbContext, scoreToAdd.PlayerId, token))
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
            BeatLeaderScore beatLeaderScore => await UpdateScoreIfChangedAsync(beatLeaderScore, dbContext),
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

        /* The score seems to be the exact same, no need to update.
         * It might just be a full score recalculation/refresh (no need to update).
         * PS: This also fixes .ScoreStatistics null causing .Discriminator changed crash. */
        if (score.Id == oldScore.Id && score.BeatLeaderScoreId == oldScore.BeatLeaderScoreId)
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

        /* The score seems to be the exact same, no need to update.
         * It might just be a full score recalculation/refresh (no need to update). */
        if (score.Id == oldScore.Id && score.ScoreSaberScoreId == oldScore.ScoreSaberScoreId)
            return oldScore;

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
            .ToArray() as IEnumerable<RankedMapId>;
        var rankedScores = await dbContext.RankedScores
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId && rankedMapsIds.Contains(x.RankedMapId))
            .ToArrayAsync();

        var mapVersions = rankedMaps.SelectMany(x => x.MapVersions).ToArray();
        var songDifficultyIds = mapVersions.Select(x => x.SongDifficultyId).ToArray() as IEnumerable<SongDifficultyId>;

        // We grab all the scores that are related to the Ranked maps, not just the ranked map version.
        var scores = await dbContext.Scores
            .Where(x => x.PlayerId == playerId && songDifficultyIds.Contains(x.SongDifficultyId))
            .ToArrayAsync();

        return new ScoreRankingContext(
            rankedMaps, rankedScores, contextsWithPointsForPlayer, scores
        );
    }

    private static IEnumerable<RankedScore> IterateRankedScoresWithTransform(
        ScoreRankingContext rankingContext,
        Func<RankedScoreTransformContext, RankedScore?, RankedScore> transform)
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
                yield return transform(transformContext, null);
        }
    }

    private static RankedScore RecalculateRankedScore(RankedScoreTransformContext context, RankedScore? rankedScore)
    {
        var effectiveScore = ScoringUtils.CalculateScoreFromModifiers(
            context.Score.BaseScore,
            context.Score.Modifiers,
            context.Point.ModifierValues
        );

        var type = ScoringUtils.RecalculateRankedScoreType(
            rankedScore,
            context.Score,
            context.Map.Requirements,
            context.SongDifficulty.Stats,
            out var invalidReason
        );

        var rawPoints = ScoringUtils.CalculateRawPoints(
            context.Score.BaseScore,
            effectiveScore,
            context.SongDifficulty.Stats.MaxScore,
            context.Point,
            context.Map.Rating
        );

        return type switch
        {
            RankedScore.ERankedScoreType.Valid => CreateValidRankedScore(context, rankedScore, effectiveScore,
                rawPoints),
            RankedScore.ERankedScoreType.Accepted => CreateAcceptedRankedScore(context, rankedScore, effectiveScore,
                rawPoints),
            RankedScore.ERankedScoreType.Pending => CreatePendingRankedScore(context, rankedScore, effectiveScore,
                rawPoints),
            RankedScore.ERankedScoreType.Refused => CreateRefusedRankedScore(context, rankedScore, effectiveScore,
                rawPoints),
            RankedScore.ERankedScoreType.Invalid => CreateInvalidRankedScore(context, rankedScore, effectiveScore,
                invalidReason),
            _ => throw new UnreachableException()
        };
    }

    private static ValidRankedScore CreateValidRankedScore(
        RankedScoreTransformContext context, RankedScore? rankedScore, EffectiveScore effectiveScore,
        RawPoints rawPoints) => new()
    {
        Id = rankedScore?.Id ?? default,
        GuildId = context.Map.GuildId,
        ContextId = context.Map.ContextId,
        RankedMapId = context.Map.Id,
        SongDifficultyId = context.SongDifficulty.Id,
        PointId = context.Point.Id,
        PlayerId = context.Score.PlayerId,
        ScoreId = context.Score.Id,
        PrevScoreId = rankedScore?.PrevScoreId,
        IsSelected = rankedScore?.IsSelected ?? false,
        EffectiveScore = effectiveScore,
        RawPoints = rawPoints,
        Rank = rankedScore is PointGivingRankedScore pointGivingRankedScore ? pointGivingRankedScore.Rank : 0,
        EditedAt = rankedScore?.EditedAt ?? context.Score.SetAt,
        Score = context.Score
    };

    private static AcceptedRankedScore CreateAcceptedRankedScore(
        RankedScoreTransformContext context, RankedScore? rankedScore, EffectiveScore effectiveScore,
        RawPoints rawPoints) => new()
    {
        Id = rankedScore?.Id ?? default,
        GuildId = context.Map.GuildId,
        ContextId = context.Map.ContextId,
        RankedMapId = context.Map.Id,
        SongDifficultyId = context.SongDifficulty.Id,
        PointId = context.Point.Id,
        PlayerId = context.Score.PlayerId,
        ScoreId = context.Score.Id,
        PrevScoreId = rankedScore?.PrevScoreId,
        IsSelected = rankedScore?.IsSelected ?? false,
        EffectiveScore = effectiveScore,
        RawPoints = rawPoints,
        Rank = rankedScore is PointGivingRankedScore pointGivingRankedScore ? pointGivingRankedScore.Rank : 0,
        EditedAt = rankedScore?.EditedAt ?? context.Score.SetAt,
        Score = context.Score
    };

    private static PendingRankedScore CreatePendingRankedScore(
        RankedScoreTransformContext context, RankedScore? rankedScore, EffectiveScore effectiveScore,
        RawPoints rawPoints) => new()
    {
        Id = rankedScore?.Id ?? default,
        GuildId = context.Map.GuildId,
        ContextId = context.Map.ContextId,
        RankedMapId = context.Map.Id,
        SongDifficultyId = context.SongDifficulty.Id,
        PointId = context.Point.Id,
        PlayerId = context.Score.PlayerId,
        ScoreId = context.Score.Id,
        PrevScoreId = rankedScore?.PrevScoreId,
        IsSelected = rankedScore?.IsSelected ?? false,
        EffectiveScore = effectiveScore,
        RawPoints = rawPoints,
        EditedAt = rankedScore?.EditedAt ?? context.Score.SetAt,
        Score = context.Score
    };

    private static RefusedRankedScore CreateRefusedRankedScore(
        RankedScoreTransformContext context, RankedScore? rankedScore, EffectiveScore effectiveScore,
        RawPoints rawPoints) => new()
    {
        Id = rankedScore?.Id ?? default,
        GuildId = context.Map.GuildId,
        ContextId = context.Map.ContextId,
        RankedMapId = context.Map.Id,
        SongDifficultyId = context.SongDifficulty.Id,
        PointId = context.Point.Id,
        PlayerId = context.Score.PlayerId,
        ScoreId = context.Score.Id,
        PrevScoreId = rankedScore?.PrevScoreId,
        IsSelected = rankedScore?.IsSelected ?? false,
        EffectiveScore = effectiveScore,
        RawPoints = rawPoints,
        EditedAt = rankedScore?.EditedAt ?? context.Score.SetAt,
        Score = context.Score
    };

    private static InvalidRankedScore CreateInvalidRankedScore(
        RankedScoreTransformContext context, RankedScore? rankedScore, EffectiveScore effectiveScore,
        EInvalidReason invalidReason) => new()
    {
        Id = rankedScore?.Id ?? default,
        GuildId = context.Map.GuildId,
        ContextId = context.Map.ContextId,
        RankedMapId = context.Map.Id,
        SongDifficultyId = context.SongDifficulty.Id,
        PointId = context.Point.Id,
        PlayerId = context.Score.PlayerId,
        ScoreId = context.Score.Id,
        PrevScoreId = rankedScore?.PrevScoreId,
        IsSelected = rankedScore?.IsSelected ?? false,
        EffectiveScore = effectiveScore,
        InvalidReason = invalidReason,
        EditedAt = rankedScore?.EditedAt ?? context.Score.SetAt,
        Score = context.Score
    };

    /// <summary>
    /// Processes ranked scores by grouping them by RankedMapId and PointId.
    /// For each group, this method:
    /// <list type="bullet">
    ///     <item>
    ///         <description>Finds the highest-scoring entry (using default comparison)</description>
    ///     </item>
    ///     <item>
    ///         <description>Clears selection from all persisted scores</description>
    ///     </item>
    ///     <item>
    ///         <description>Sets only the best score as selected</description>
    ///     </item>
    /// </list>
    /// This ensures that for each (RankedMapId, PointId) combination, only one score is marked as selected.
    /// </summary>
    /// <param name="rankedScores">Collection of ranked scores to process</param>
    /// <returns>Processed collection of ranked scores with appropriate selection state</returns>
    internal static IEnumerable<RankedScore> SetStateForBestRankedScorePerGroup(IEnumerable<RankedScore> rankedScores)
        => rankedScores
            .GroupBy(x => (x.RankedMapId, x.PointId))
            .SelectMany(group => group.Max() switch
            {
                null => throw new InvalidOperationException("Group should contain at least one element."),
                var best => group.Select(x => x == best ? SelectRankedScore(x) : DeselectRankedScore(x))
            });

    private static RankedScore DeselectRankedScore(RankedScore rankedScore)
    {
        rankedScore.IsSelected = false;
        return rankedScore;
    }

    private static RankedScore SelectRankedScore(RankedScore rankedScore)
    {
        rankedScore.IsSelected = true;
        return rankedScore;
    }

    private static ValueTask<Context[]> GetContextWithPointsForPlayerAsync(
        PlayerId playerId, HybridCache cache, IServiceScopeFactory scopeFactory)
        => cache.GetOrCreateAsync($"{ContextWithPointsForPlayerCacheKeyPrefix}{playerId}", (scopeFactory, playerId),
            async static (state, token) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await _getContextWithPointsForPlayerQuery(dbContext, state.playerId)
                    .ToArrayAsync(token);
            }, _cacheEntryOptions);
}