using System.Diagnostics;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Api.Features.Scores.Calculations;
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
    HybridCache cache,
    PointCurveService pointCurves)
{
    private const string ContextWithPointsForPlayerCacheKeyPrefix = "ContextWithPointsForPlayer_";
    internal const string PlayerContextsCacheTag = "ScoringPlayerContexts";

    private static readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5)
    };

    private static readonly string[] _cacheTags = [PlayerContextsCacheTag];

    private static readonly Func<ServerDbContext, PlayerId, IAsyncEnumerable<Context>>
        _getContextWithPointsForPlayerQuery = EF.CompileAsyncQuery((ServerDbContext dbContext, PlayerId playerId)
            => dbContext.ContextMembers
                .Where(x => x.PlayerId == playerId)
                .Include(x => x.Context).ThenInclude(x => x.Points)
                .Select(x => x.Context));

    /// <summary>
    /// Check if there is the same BeatLeader score already existing, ignoring the BeatLeaderScoreId.
    /// </summary>
    private static readonly Func<ServerDbContext, PlayerId, SongDifficultyId, DateTimeOffset, BaseScore,
        CancellationToken, Task<BeatLeaderScore?>> _findExistingBeatLeaderScoreIgnoringBLScoreIdQuery =
        EF.CompileAsyncQuery((
                ServerDbContext dbContext, PlayerId playerId,
                SongDifficultyId songDifficultyId,
                DateTimeOffset setAt, BaseScore baseScore, CancellationToken token)
            => dbContext.BeatLeaderScores.FirstOrDefault(x =>
                x.PlayerId == playerId
                && x.SongDifficultyId == songDifficultyId
                && x.SetAt - setAt < TimeSpan.FromSeconds(2)
                && x.BaseScore == baseScore));

    private static readonly Func<ServerDbContext, PlayerId, SongDifficultyId, DateTimeOffset, BaseScore,
        CancellationToken, Task<ScoreSaberScore?>> _findExistingScoreSaberScoreQuery = EF.CompileAsyncQuery((
            ServerDbContext dbContext, PlayerId playerId,
            SongDifficultyId songDifficultyId,
            DateTimeOffset setAt, BaseScore baseScore, CancellationToken token)
        => dbContext.ScoreSaberScores.FirstOrDefault(x =>
            x.PlayerId == playerId
            && x.SongDifficultyId == songDifficultyId
            && x.SetAt - setAt < TimeSpan.FromSeconds(2)
            && x.BaseScore == baseScore));

    private record ScoreRankingContext(
        RankedMap[] RankedMapsWithVersionsWithSongDifficulty,
        RankedScore[] ExistingRankedScores,
        Context[] ContextsWithPoints,
        AbstractScore[] Scores,
        IReadOnlyDictionary<(RankedMapId, Point.PointId), RankedMapPointCurve> Curves,
        AbstractScore? PreviousScore = null
    );

    private readonly record struct ScoreUpdate(AbstractScore Score, AbstractScore? PreviousScore);

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
        AbstractScore Score,
        CustomCurve? AccuracyCurve
    );

    public readonly record struct PipelineResult(Context[] ImpactedContextsWithPoints);

    public ValueTask ClearPlayerCacheAsync(PlayerId playerId, CancellationToken token = default)
        => cache.RemoveAsync($"{ContextWithPointsForPlayerCacheKeyPrefix}{playerId}", token);

    /// <remarks>
    /// There is codebase assumption that all scores going in are stored and updated by default, don't change this behavior.
    /// </remarks>
    public async Task<PipelineResult> ExecuteAsync(
        AbstractScore scoreToAdd, bool refreshCurveBenchmarks = false, CancellationToken token = default)
    {
        if (!(await UpdateScoreIfChangedAsync(scoreToAdd, dbContext, token)).TryGetValue(out var score))
            score = await dbContext.AddAndSaveAsync(scoreToAdd, static score => new ScoreUpdate(score, null), token);

        var (rankingContext, rankedScores) = await dbContext.Database
            .CreateExecutionStrategy()
            .ExecuteAsync((score, dbContext, scopeFactory, cache), static (state, token) =>
                    RecalculateRankedScoresAsync(state.score, state.dbContext, state.scopeFactory, state.cache, token),
                token);

        await MaybeQueueCurveRefreshesAsync(rankingContext, rankedScores, refreshCurveBenchmarks, pointCurves, token);

        return new PipelineResult(rankingContext.ContextsWithPoints);
    }

    private static async Task<(ScoreRankingContext rankingContext, RankedScore[] rankedScores)>
        RecalculateRankedScoresAsync(
            ScoreUpdate update, ServerDbContext dbContext,
            IServiceScopeFactory scopeFactory, HybridCache cache, CancellationToken token)
    {
        var (contextsWithPoints, rankedMapIds) = await GetScoreRankingTargetsAsync(
            update.Score, dbContext, scopeFactory, cache, token
        );

        if (rankedMapIds.Length == 0)
        {
            dbContext.ChangeTracker.Clear();

            return (new ScoreRankingContext([], [], contextsWithPoints, [],
                new Dictionary<(RankedMapId, Point.PointId), RankedMapPointCurve>()), []);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(token);

        try
        {
            await RankedScoreUpdateRankPipeline.LockMapsAsync(rankedMapIds, dbContext, token);

            var rankingContext = await PrepareScoreRankingContextAsync(
                update, rankedMapIds, contextsWithPoints, dbContext, token);

            var rankedScores = SetStateForBestRankedScorePerGroup(
                    IterateRankedScoresWithTransform(rankingContext, RecalculateRankedScore))
                .ToArray();

            await SaveRankedScoresAsync(rankedScores, dbContext, token);
            await RankedScoreUpdateRankPipeline.UpdateLockedMapRanksAsync(
                rankingContext.RankedMapsWithVersionsWithSongDifficulty.Select(x => x.Id), dbContext, null, token);

            await transaction.CommitAsync(token);

            return (rankingContext, rankedScores);
        }
        finally
        {
            // A retried transaction must start without entities from its rolled-back attempt.
            dbContext.ChangeTracker.Clear();
        }
    }

    private static async ValueTask MaybeQueueCurveRefreshesAsync(
        ScoreRankingContext rankingContext, RankedScore[] rankedScores, bool refreshCurveBenchmarks,
        PointCurveService pointCurves, CancellationToken token)
    {
        if (rankedScores.Length == 0)
            return;

        // Queue after commit because heavy imports can execute this work inline in another scope.
        foreach (var group in rankedScores.GroupBy(x => (x.RankedMapId, x.PointId)))
            if (ShouldRefreshCurve(rankingContext, group.Key, group, refreshCurveBenchmarks))
                await pointCurves.QueueRefreshAsync(group.Key.RankedMapId, group.Key.PointId, token);
    }

    private static async Task<(Context[] ContextsWithPoints, RankedMapId[] RankedMapIds)> GetScoreRankingTargetsAsync(
        AbstractScore score, ServerDbContext dbContext, IServiceScopeFactory scopeFactory,
        HybridCache cache, CancellationToken token)
    {
        var contextsWithPoints = await GetContextWithPointsForPlayerAsync(score.PlayerId, cache, scopeFactory, token);

        if (contextsWithPoints.Length == 0)
            return (contextsWithPoints, []);

        var contextIds = contextsWithPoints
            .Select(x => x.Id)
            .ToArray();

        var rankedMapIds = await dbContext.RankedMaps
            .Where(x =>
                ((IEnumerable<ContextId>)contextIds).Contains(x.ContextId)
                && x.MapVersions.Any(v => v.SongDifficultyId == score.SongDifficultyId))
            .Select(x => x.Id)
            .ToArrayAsync(token);

        return (contextsWithPoints, rankedMapIds);
    }

    /// <summary>Recalculates a player's ranked scores for one map and point type.</summary>
    /// <returns>Whether the player's selected pass eligibility changed.</returns>
    public Task<bool> RecalculatePlayerScoresForMapAsync(
        RankedMapId mapId, Point.PointId pointId, PlayerId playerId, bool updateRanks = true,
        CancellationToken token = default)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (dbContext, mapId, pointId, playerId, updateRanks),
            static async (state, token) =>
            {
                await using var transaction = await state.dbContext.Database.BeginTransactionAsync(token);

                try
                {
                    await RankedScoreUpdateRankPipeline.LockMapsAsync([state.mapId], state.dbContext, token);

                    var passChanged = await RecalculateRankedScoresAsync(
                        state.mapId, state.pointId, state.playerId, state.updateRanks,
                        state.dbContext, token);

                    await transaction.CommitAsync(token);

                    return passChanged;
                }
                finally
                {
                    state.dbContext.ChangeTracker.Clear();
                }
            }, token);

    private static async Task<bool> RecalculateRankedScoresAsync(
        RankedMapId mapId, Point.PointId pointId, PlayerId playerId, bool updateRanks,
        ServerDbContext dbContext, CancellationToken token)
    {
        var map = await dbContext.RankedMaps
            .Include(x => x.MapVersions)
            .ThenInclude(x => x.SongDifficulty)
            .SingleOrDefaultAsync(x => x.Id == mapId, token);

        if (map is null) return false;

        if (!await dbContext.ContextMembers
                .AnyAsync(x => x.ContextId == map.ContextId && x.PlayerId == playerId, token))
            return false;

        var context = await dbContext.Contexts
            .Include(x => x.Points.Where(p => p.Id == pointId))
            .SingleAsync(x => x.Id == map.ContextId, token);

        var point = context.Points.SingleOrDefault(x => x.Id == pointId);

        if (point is null) return false;

        context.Points = [point];

        var difficultyIds = map.MapVersions
            .Select(x => x.SongDifficultyId)
            .ToArray();

        var scores = await dbContext.Scores
            .Where(x =>
                x.PlayerId == playerId
                && ((IEnumerable<SongDifficultyId>)difficultyIds).Contains(x.SongDifficultyId))
            .ToArrayAsync(token);

        var existing = await dbContext.RankedScores
            .Where(x =>
                x.RankedMapId == mapId
                && x.PointId == pointId
                && x.PlayerId == playerId)
            .ToArrayAsync(token);

        var hadSelectedPass = existing.Any(x => x.IsSelected && x is PointGivingRankedScore);

        var curves = new Dictionary<(RankedMapId, Point.PointId), RankedMapPointCurve>();

        if (point.CurveSettings.AccuracyBonusRatio > 0 && point.CurveSettings.MapCurvePolicy != PointCurvePolicy.Shared)
        {
            var curve = await dbContext.RankedMapPointCurves
                .Where(x => x.RankedMapId == mapId && x.PointId == pointId)
                .SingleOrDefaultAsync(token);

            if (curve is not null) curves.Add((mapId, pointId), curve);
        }

        var rankingContext = new ScoreRankingContext([map], existing, [context], scores, curves);
        var rankedScores = SetStateForBestRankedScorePerGroup(
                IterateRankedScoresWithTransform(rankingContext, RecalculateRankedScore))
            .ToArray();

        await SaveRankedScoresAsync(rankedScores, dbContext, token);

        if (updateRanks)
            await RankedScoreUpdateRankPipeline.UpdateLockedMapRanksAsync([mapId], dbContext, pointId, token);

        return hadSelectedPass != rankedScores.Any(x => x.IsSelected && x is PointGivingRankedScore);
    }

    private static Task SaveRankedScoresAsync(
        RankedScore[] rankedScores, ServerDbContext dbContext, CancellationToken token)
    {
        // Raw scores are saved separately. Keep their navigation properties out of the tracked graph.
        foreach (var rankedScore in rankedScores)
            rankedScore.Score = null!;

        dbContext.RankedScores.UpdateRange(rankedScores);

        return dbContext.SaveChangesAsync(token);
    }

    /// <summary>
    /// There is cases when Scores are processed and sent again from BeatLeader but with a score ID.
    /// In that case we want to update the existing score with the new ID if it doesn't already have one.
    /// There is also cases when the score is submitted multiple times, we don't want to create duplicates.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="dbContext">
    /// The database context to use for the operation.
    /// </param>
    /// <param name="token">The cancellation token for the database operations.</param>
    private static async Task<Maybe<ScoreUpdate>> UpdateScoreIfChangedAsync(
        AbstractScore score, ServerDbContext dbContext, CancellationToken token)
        => score switch
        {
            BeatLeaderScore beatLeaderScore => await UpdateScoreIfChangedAsync(beatLeaderScore, dbContext, token),
            ScoreSaberScore saberScore => await UpdateScoreIfChangedAsync(saberScore, dbContext, token),
            _ => throw new InvalidOperationException("Score must be either a BeatLeaderScore or a ScoreSaberScore.")
        };

    /// <summary>
    /// Update the BeatLeader score if it already exists without an ID.
    /// If it doesn't exist, return None.
    /// If it exists and has no ID, just return the old score.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="dbContext"></param>
    /// <param name="token">The cancellation token for the database operations.</param>
    /// <remarks>
    /// BeatLeader can send the same score multiple times, first without a BLScoreId, then with it.
    /// We want to avoid creating duplicates (when sent multiple times),
    /// and we also want to update the existing score with the BLScoreId when it arrives.
    /// </remarks>
    /// <returns></returns>
    private static async Task<Maybe<ScoreUpdate>> UpdateScoreIfChangedAsync(
        BeatLeaderScore score, ServerDbContext dbContext, CancellationToken token)
    {
        var oldScore = await _findExistingBeatLeaderScoreIgnoringBLScoreIdQuery(
            dbContext,
            score.PlayerId,
            score.SongDifficultyId,
            score.SetAt,
            score.BaseScore,
            token);
        if (oldScore is null)
            return None;

        /* It was already there, but the incoming score has no scoreId, there is no need to update the score.
         * It might just be a full score recalculation/refresh (no need to update). */
        if (score.BeatLeaderScoreId is null)
            return new ScoreUpdate(oldScore, oldScore);

        /* The score seems to be the exact same, no need to update.
         * It might just be a full score recalculation/refresh (no need to update).
         * PS: This also fixes .ScoreStatistics null causing .Discriminator changed crash. */
        if (score.Id == oldScore.Id && score.BeatLeaderScoreId == oldScore.BeatLeaderScoreId)
            return new ScoreUpdate(oldScore, oldScore);

        score.Id = oldScore.Id;
        dbContext.BeatLeaderScores.Update(score);
        await dbContext.SaveChangesAsync(token);

        return new ScoreUpdate(score, oldScore);
    }

    private static async Task<Maybe<ScoreUpdate>> UpdateScoreIfChangedAsync(
        ScoreSaberScore score, ServerDbContext dbContext, CancellationToken token)
    {
        var oldScore = await _findExistingScoreSaberScoreQuery(
            dbContext,
            score.PlayerId,
            score.SongDifficultyId,
            score.SetAt,
            score.BaseScore,
            token);
        if (oldScore is null)
            return None;

        /* The score seems to be the exact same, no need to update.
         * It might just be a full score recalculation/refresh (no need to update). */
        if (score.Id == oldScore.Id && score.ScoreSaberScoreId == oldScore.ScoreSaberScoreId)
            return new ScoreUpdate(oldScore, oldScore);

        score.Id = oldScore.Id;
        dbContext.ScoreSaberScores.Update(score);
        await dbContext.SaveChangesAsync(token);

        return new ScoreUpdate(score, oldScore);
    }

    private static async Task<ScoreRankingContext> PrepareScoreRankingContextAsync(
        ScoreUpdate update, RankedMapId[] rankedMapIds, Context[] contextsWithPoints,
        ServerDbContext dbContext, CancellationToken token)
    {
        /* As a revision can change versions or requirements while we wait, we only fetched the IDs before acquiring the map locks.
         * Therefore we only then now load the maps to avoid applying scoring with outdated data. */
        var rankedMaps = await dbContext.RankedMaps
            .Include(x => x.MapVersions)
            .ThenInclude(x => x.SongDifficulty)
            .Where(x =>
                ((IEnumerable<RankedMapId>)rankedMapIds).Contains(x.Id)
                && x.MapVersions.Any(v => v.SongDifficultyId == update.Score.SongDifficultyId))
            .ToArrayAsync(token);

        return await PrepareScoreRankingContextAsync(
            update.Score.PlayerId, rankedMaps, contextsWithPoints, dbContext, update.PreviousScore, token);
    }

    private static async Task<ScoreRankingContext> PrepareScoreRankingContextAsync(
        PlayerId playerId, RankedMap[] rankedMaps, Context[] contextsWithPointsForPlayer,
        ServerDbContext dbContext, AbstractScore? previousScore = null, CancellationToken token = default)
    {
        if (rankedMaps.Length == 0)
            return new ScoreRankingContext(rankedMaps, [], contextsWithPointsForPlayer, [],
                new Dictionary<(RankedMapId, Point.PointId), RankedMapPointCurve>(), previousScore);

        var rankedMapsIds = rankedMaps
            .Select(x => x.Id)
            .ToArray();

        var rankedScores = await dbContext.RankedScores
            .Where(x =>
                x.PlayerId == playerId
                && ((IEnumerable<RankedMapId>)rankedMapsIds).Contains(x.RankedMapId))
            .ToArrayAsync(token);

        var songDifficultyIds = rankedMaps
            .SelectMany(x => x.MapVersions)
            .Select(x => x.SongDifficultyId)
            .ToArray() as IEnumerable<SongDifficultyId>;

        // We grab all the scores that are related to the Ranked maps, not just the ranked map version.
        var scores = await dbContext.Scores
            .Where(x => x.PlayerId == playerId && songDifficultyIds.Contains(x.SongDifficultyId))
            .ToArrayAsync(token);

        var pointIds = contextsWithPointsForPlayer
            .SelectMany(x => x.Points)
            .Where(x =>
                x.CurveSettings.AccuracyBonusRatio > 0
                && x.CurveSettings.MapCurvePolicy != PointCurvePolicy.Shared)
            .Select(x => x.Id)
            .ToArray();

        var curves = pointIds.Length == 0
            ? []
            : await dbContext.RankedMapPointCurves
                .Where(x =>
                    ((IEnumerable<RankedMapId>)rankedMapsIds).Contains(x.RankedMapId)
                    && ((IEnumerable<Point.PointId>)pointIds).Contains(x.PointId))
                .ToDictionaryAsync(x => (x.RankedMapId, x.PointId), token);

        return new ScoreRankingContext(rankedMaps, rankedScores, contextsWithPointsForPlayer, scores,
            curves, previousScore);
    }

    private static bool ShouldRefreshCurve(
        ScoreRankingContext context,
        (RankedMapId MapId, Point.PointId PointId) key,
        IEnumerable<RankedScore> candidates,
        bool refreshCurveBenchmarks)
    {
        var (mapId, pointId) = key;
        var point = context.ContextsWithPoints
            .SelectMany(x => x.Points)
            .First(x => x.Id == pointId);

        if (point.CurveSettings is not
            { AccuracyBonusRatio: > 0, MapCurvePolicy: PointCurvePolicy.ScoreDerivedMapCurve })
            return false;

        var map = context.RankedMapsWithVersionsWithSongDifficulty
            .Single(x => x.Id == mapId);

        var curve = context.Curves.GetValueOrDefault(key);
        var benchmarks = curve?.BenchmarkAccuracies ?? [];
        var current = BestAccuracy(context, candidates, map, pointId, beforeUpdate: false);

        // A missing override still needs bootstrapping when an existing score is replayed.
        if (curve is null) return current is not null;

        var previous = BestAccuracy(context, context.ExistingRankedScores, map, pointId,
            beforeUpdate: true, refreshCurveBenchmarks ? benchmarks : null);

        // Explicit replay also covers approvals and membership changes persisted before this call.
        if (!refreshCurveBenchmarks && Nullable.Equals(previous, current)) return false;

        // Only an entrant or a disappearing benchmark can affect a full benchmark set.
        return benchmarks.Length < point.CurveSettings.RunnerUpRewardFractions.Length + 1
               || previous >= benchmarks[^1]
               || current > benchmarks[^1];
    }

    private static double? BestAccuracy(
        ScoreRankingContext context, IEnumerable<RankedScore> entries, RankedMap map, Point.PointId pointId,
        bool beforeUpdate, double[]? formerBenchmarks = null)
    {
        double? best = null;
        SongDifficulty? difficulty = null;

        foreach (var entry in entries)
        {
            if (entry.RankedMapId != map.Id || entry.PointId != pointId
                                            || formerBenchmarks is null && entry is not PointGivingRankedScore)
                continue;

            var score = beforeUpdate && context.PreviousScore?.Id == entry.ScoreId
                ? context.PreviousScore
                : FindScore(context.Scores, entry.ScoreId);

            if (score is null || formerBenchmarks is null && score.Statistics?.WinTracker.IsWin == false
                              || score.BaseScore <= 0)
                continue;

            if (difficulty?.Id != entry.SongDifficultyId)
                difficulty = FindDifficulty(map, entry.SongDifficultyId);

            if (difficulty is null
                || !Accuracy.From(score.BaseScore, difficulty.Stats.MaxScore)
                    .TryGetValue(out var scoreAccuracy))
                continue;

            var accuracy = scoreAccuracy / 100d;

            // An already-ineligible entry matters on replay only if it could be a stored benchmark.
            if (formerBenchmarks is not null
                && (entry is not PointGivingRankedScore || score.Statistics?.WinTracker.IsWin == false)
                && !formerBenchmarks.Contains(accuracy))
                continue;

            if (best is null || accuracy > best) best = accuracy;
        }

        return best;
    }

    private static AbstractScore? FindScore(ReadOnlySpan<AbstractScore> scores, ScoreId scoreId)
    {
        foreach (var score in scores)
            if (score.Id == scoreId)
                return score;

        return null;
    }

    private static SongDifficulty? FindDifficulty(RankedMap map, SongDifficultyId difficultyId)
        => map.MapVersions.FirstOrDefault(x => x.SongDifficultyId == difficultyId)?.SongDifficulty;

    private static IEnumerable<RankedScore> IterateRankedScoresWithTransform(
        ScoreRankingContext rankingContext,
        Func<RankedScoreTransformContext, RankedScore?, RankedScore> transform)
    {
        foreach (var rankedMap in rankingContext.RankedMapsWithVersionsWithSongDifficulty)
            // Ranked-score matching doesn't include play modes, so we process each difficulty once per ranked map.
        foreach (var mapVersion in rankedMap.MapVersions.DistinctBy(x => x.SongDifficultyId))
        foreach (var point in rankingContext.ContextsWithPoints.First(x => x.Id == rankedMap.ContextId).Points)
        foreach (var score in rankingContext.Scores.Where(x => x.SongDifficultyId == mapVersion.SongDifficultyId))
        {
            var transformContext = new RankedScoreTransformContext(
                rankedMap,
                mapVersion.SongDifficulty,
                point,
                score,
                rankingContext.Curves.GetValueOrDefault((rankedMap.Id, point.Id))?.Curve
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
            context.Map.Rating,
            context.AccuracyCurve
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
        PlayerId playerId, HybridCache cache, IServiceScopeFactory scopeFactory, CancellationToken token)
        => cache.GetOrCreateAsync($"{ContextWithPointsForPlayerCacheKeyPrefix}{playerId}", (scopeFactory, playerId),
            async static (state, cancellationToken) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await _getContextWithPointsForPlayerQuery(dbContext, state.playerId)
                    .ToArrayAsync(cancellationToken);
            }, _cacheEntryOptions, tags: _cacheTags, cancellationToken: token);
}