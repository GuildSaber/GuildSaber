using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.PointCurves.Http;
using GuildSaber.Api.Features.Scores.Calculations;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using NpgsqlTypes;

namespace GuildSaber.Api.Features.PointCurves;

public sealed class PointCurveService(
    ServerDbContext db,
    TimeProvider clock,
    IHeavyBackgroundTaskQueue queue,
    IServiceScopeFactory scopes,
    HybridCache cache)
{
    private const int RankedMapBatchSize = 128;
    public const double SamplingTolerance = 0.001;
    internal sealed record Scope(Point Point, RankedMap Map, RankedMapPointCurve? Override);

    public async Task<Result<PointCurveResponses.Settings, PointCurveError>> GetSettingsAsync(
        GuildId guildId, Point.PointId pointId, CancellationToken token)
        => await db.Points
            .SingleOrDefaultAsync(x => x.Id == pointId && x.GuildId == guildId, token) is { } point
            ? Settings(point.CurveSettings)
            : Failure<PointCurveResponses.Settings, PointCurveError>(new PointCurveError.NotFound());

    public async Task<Result<PointCurveResponses.Settings, PointCurveError>> UpdateSettingsAsync(
        GuildId guildId, Point.PointId pointId, PointCurveRequests.UpdateSettings request, CancellationToken token)
    {
        if (!ValidateSettings(request).TryGetValue(out var settings, out var error))
            return Failure<PointCurveResponses.Settings, PointCurveError>(
                new PointCurveError.Validation("curveSettings", error));

        var updated = await db.Database.CreateExecutionStrategy()
            .ExecuteAsync(
                (service: this, guildId, pointId, settings),
                static (state, cancellationToken) => state.service.SaveSettingsAsync(
                    state.guildId, state.pointId, state.settings, cancellationToken),
                token);

        if (!updated.TryGetValue(out var regenerate, out var updateError))
            return Failure<PointCurveResponses.Settings, PointCurveError>(updateError);

        // Shared settings are cached with player contexts; individual map curves are loaded separately.
        await cache.RemoveByTagAsync(ScoreAddOrUpdatePipeline.PlayerContextsCacheTag, token);

        await QueueAsync(async cancellationToken =>
        {
            await using var scope = scopes.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            var pipeline = scope.ServiceProvider.GetRequiredService<MapPointCurvePipeline>();
            var memberStatsPipeline = scope.ServiceProvider.GetRequiredService<MemberStatsPipeline>();
            long cursor = 0;

            while (true)
            {
                var afterMapId = new RankedMapId(cursor);
                var maps = await database.RankedMaps
                    .Where(x =>
                        x.Id > afterMapId
                        && x.Context.Points.Any(p => p.Id == pointId)
                        && (x.RankedScores.Any(s => s.PointId == pointId)
                            || database.RankedMapPointCurves.Any(c =>
                                c.RankedMapId == x.Id && c.PointId == pointId)
                            || x.MapVersions.Any(v => database.Scores.Any(s =>
                                s.SongDifficultyId == v.SongDifficultyId
                                && database.ContextMembers.Any(m =>
                                    m.ContextId == x.ContextId && m.PlayerId == s.PlayerId)))))
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .Take(RankedMapBatchSize)
                    .ToArrayAsync(cancellationToken);

                if (maps.Length == 0)
                    break;

                foreach (var mapId in maps)
                    await pipeline.ExecuteAsync(
                        mapId, pointId, regenerate, refreshMemberStats: false, token: cancellationToken);

                cursor = maps[^1].Value;
            }

            await memberStatsPipeline.RecalculateForPointAsync(pointId, cancellationToken);
        }, token);

        return Settings(settings);
    }

    private async Task<Result<bool, PointCurveError>> SaveSettingsAsync(
        GuildId guildId, Point.PointId pointId, CurveSettings settings, CancellationToken token)
    {
        db.ChangeTracker.Clear();

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var point = await db.Points
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == pointId && x.GuildId == guildId, token);

        if (point is null)
            return Failure<bool, PointCurveError>(new PointCurveError.NotFound());

        var clearOverrides = point.CurveSettings.MapCurvePolicy != settings.MapCurvePolicy
                             || !point.CurveSettings.RunnerUpRewardFractions.AsSpan()
                                 .SequenceEqual(settings.RunnerUpRewardFractions);
        var regenerate = clearOverrides
                         || point.CurveSettings.AccuracyBonusRatio == 0 && settings.AccuracyBonusRatio > 0;

        point.CurveSettings = settings;
        await db.SaveChangesAsync(token);
        db.ChangeTracker.Clear();

        if (clearOverrides)
            await db.RankedMapPointCurves
                .Where(x => x.PointId == pointId)
                .ExecuteDeleteAsync(token);

        await transaction.CommitAsync(token);

        return regenerate;
    }

    public async Task<Result<PointCurveResponses.Curve, PointCurveError>> GetAsync(
        ContextId contextId, RankedMapId mapId, Point.PointId pointId, CancellationToken token)
        => await LoadAsync(contextId, mapId, pointId, token) is { } scope
            ? Response(scope)
            : Failure<PointCurveResponses.Curve, PointCurveError>(new PointCurveError.NotFound());

    public async Task<Result<PointCurveResponses.Curve, PointCurveError>> PutAsync(
        ContextId contextId, RankedMapId mapId, Point.PointId pointId,
        PointCurveRequests.PutCurve request, CancellationToken token)
    {
        if (request.Points is not { Length: >= 2 and <= AccuracyCurveMath.MaximumPoints })
            return Failure<PointCurveResponses.Curve, PointCurveError>(
                new PointCurveError.Validation("points", "An accuracy curve requires 2..256 samples."));

        if (!AccuracyCurveMath.Validate(Array.ConvertAll(request.Points, x => new CurvePoint(x.X, x.Y)))
                .TryGetValue(out var curve, out var error))
            return Failure<PointCurveResponses.Curve, PointCurveError>(
                new PointCurveError.Validation("points", error));

        var scope = await LoadAsync(contextId, mapId, pointId, token);

        if (scope is null)
            return Failure<PointCurveResponses.Curve, PointCurveError>(new PointCurveError.NotFound());

        if (scope.Point.CurveSettings.MapCurvePolicy != PointCurvePolicy.EditableMapCurve)
            return Failure<PointCurveResponses.Curve, PointCurveError>(
                new PointCurveError.ReadOnly("This point's map curves are not editable."));

        if (await SaveAsync(scope, curve, [], token))
            await QueueRecalculationAsync(mapId, pointId, false, token: token);

        return Response(await LoadAsync(contextId, mapId, pointId, token) ?? scope);
    }

    public async Task<Result<PointCurveResponses.Curve, PointCurveError>> DeleteAsync(
        ContextId contextId, RankedMapId mapId, Point.PointId pointId, CancellationToken token)
    {
        var scope = await LoadAsync(contextId, mapId, pointId, token);

        if (scope is null)
            return Failure<PointCurveResponses.Curve, PointCurveError>(new PointCurveError.NotFound());

        if (scope.Point.CurveSettings.MapCurvePolicy != PointCurvePolicy.EditableMapCurve)
            return Failure<PointCurveResponses.Curve, PointCurveError>(
                new PointCurveError.ReadOnly("This point's map curves are not editable."));

        if (await SaveAsync(scope, curve: null, [], token))
            await QueueRecalculationAsync(mapId, pointId, false, token: token);

        return Response(scope with { Override = null });
    }

    public async Task<Result<PointCurveResponses.Preview, PointCurveError>> PreviewAsync(
        ContextId contextId, RankedMapId mapId, Point.PointId pointId,
        PointCurveRequests.Preview request, CancellationToken token)
    {
        var scope = await LoadAsync(contextId, mapId, pointId, token);

        if (scope is null)
            return Failure<PointCurveResponses.Preview, PointCurveError>(new PointCurveError.NotFound());

        if (request.Accuracies is { Length: > 256 })
            return Failure<PointCurveResponses.Preview, PointCurveError>(
                new PointCurveError.Validation("accuracies", "At most 256 accuracies are allowed."));

        if (request.Points is { Length: < 2 or > AccuracyCurveMath.MaximumPoints })
            return Failure<PointCurveResponses.Preview, PointCurveError>(
                new PointCurveError.Validation("points", "An accuracy curve requires 2..256 samples."));

        var curve = scope.Override?.Curve ?? scope.Point.CurveSettings.Accuracy;

        if (request.Points is not null
            && !AccuracyCurveMath.Validate(Array.ConvertAll(request.Points, x => new CurvePoint(x.X, x.Y)))
                .TryGetValue(out curve, out var error))
            return Failure<PointCurveResponses.Preview, PointCurveError>(
                new PointCurveError.Validation("points", error));

        var basePoints = scope.Point.CurveSettings.Difficulty.ProjectValue(scope.Map.Rating.DiffStar);
        var ratio = scope.Point.CurveSettings.AccuracyBonusRatio;
        var values = new List<PointCurveResponses.PreviewValue>();

        foreach (var fraction in request.Accuracies ?? [0, 0.5, 0.9, 1])
        {
            if (!Accuracy.TryCreate(fraction * 100d).TryGetValue(out var accuracy, out var accuracyError))
                return Failure<PointCurveResponses.Preview, PointCurveError>(
                    new PointCurveError.Validation("accuracies", accuracyError));

            if (!AccuracyCurveMath.Calculate(basePoints, ratio, accuracy, curve)
                    .TryGetValue(out var raw, out var calculationError))
                return Failure<PointCurveResponses.Preview, PointCurveError>(
                    new PointCurveError.Validation("accuracies", calculationError));

            values.Add(new PointCurveResponses.PreviewValue(
                fraction, ratio == 0 ? 0 : AccuracyCurveMath.Evaluate(curve, accuracy), raw));
        }

        return new PointCurveResponses.Preview(
            Response(scope) with { Points = Samples(curve) },
            basePoints,
            ratio,
            basePoints * ratio,
            [.. values]);
    }

    public ValueTask QueueRefreshAsync(RankedMapId mapId, Point.PointId pointId, CancellationToken token)
        => QueueAsync(async cancellationToken =>
        {
            await using var scope = scopes.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<PointCurveService>();

            if (await service.RefreshAsync(mapId, pointId, cancellationToken))
                await scope.ServiceProvider.GetRequiredService<MapPointCurvePipeline>()
                    .ExecuteAsync(mapId, pointId, false, token: cancellationToken);
        }, token);

    public ValueTask QueueRecalculationAsync(
        RankedMapId mapId, Point.PointId pointId, bool regenerate, PlayerId? affectedPlayer = null,
        CancellationToken token = default)
        => QueueAsync(async cancellationToken =>
        {
            await using var scope = scopes.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<MapPointCurvePipeline>()
                .ExecuteAsync(mapId, pointId, regenerate, affectedPlayer, token: cancellationToken);
        }, token);

    // Bulk imports already running on this queue must not wait for space in their own queue.
    private ValueTask QueueAsync(Func<CancellationToken, ValueTask> work, CancellationToken token)
        => HeavyQueueProcessingService.IsProcessingWorkItem
            ? work(token)
            : queue.QueueBackgroundWorkItemAsync(work, token);

    internal async Task<bool> RefreshAsync(RankedMapId mapId, Point.PointId pointId, CancellationToken token)
    {
        var context = await db.RankedMaps
            .Where(x => x.Id == mapId)
            .Select(x => (ContextId?)x.ContextId)
            .SingleOrDefaultAsync(token);

        if (context is null) return false;

        var scope = await LoadAsync(context.Value, mapId, pointId, token);

        if (scope is null
            || scope.Point.CurveSettings.MapCurvePolicy != PointCurvePolicy.ScoreDerivedMapCurve
            || scope.Point.CurveSettings.AccuracyBonusRatio == 0)
            return false;

        var count = scope.Point.CurveSettings.RunnerUpRewardFractions.Length + 1;

        // Match Accuracy.From followed by normalization for exact benchmark comparisons.
        var accuracies = await db.Database.SqlQuery<double>(
                $"""
                 SELECT "Accuracy" AS "Value"
                 FROM (
                     SELECT (score."BaseScore"::double precision / difficulty."Stats_MaxScore" * 100) / 100 AS "Accuracy",
                         ROW_NUMBER() OVER (
                             PARTITION BY ranked."PlayerId"
                             ORDER BY (score."BaseScore"::double precision / difficulty."Stats_MaxScore" * 100) / 100 DESC,
                                 score."Id"
                         ) AS n
                     FROM "RankedScores" ranked
                     JOIN "Scores" score ON score."Id" = ranked."ScoreId"
                     JOIN "SongDifficulties" difficulty ON difficulty."Id" = score."SongDifficultyId"
                     WHERE ranked."RankedMapId" = {mapId.Value}
                         AND ranked."PointId" = {pointId.Value}
                         AND ranked."Type" IN (0,3)
                         AND difficulty."Stats_MaxScore" > 0
                         AND score."WinTracker_IsWin" IS DISTINCT FROM false
                         AND score."BaseScore" > 0
                         AND score."BaseScore" <= difficulty."Stats_MaxScore"
                         AND EXISTS (
                             SELECT 1
                             FROM "MapVersions" version
                             WHERE version."RankedMapId" = ranked."RankedMapId"
                                 AND version."SongDifficultyId" = ranked."SongDifficultyId"
                         )
                         AND EXISTS (
                             SELECT 1
                             FROM "ContextMembers" member
                             WHERE member."ContextId" = ranked."ContextId"
                                 AND member."PlayerId" = ranked."PlayerId"
                         )
                 ) players
                 WHERE n = 1
                 ORDER BY "Accuracy" DESC
                 LIMIT {count}
                 """)
            .ToArrayAsync(token);

        if (accuracies.Length == 0) return await SaveAsync(scope, null, [], token);

        if (scope.Override is not null && accuracies.AsSpan().SequenceEqual(scope.Override.BenchmarkAccuracies))
            return false;

        var generated = NativeCurveGenerator.Generate(
            accuracies, scope.Point.CurveSettings.RunnerUpRewardFractions, SamplingTolerance);

        if (generated.IsFailure) throw new InvalidOperationException(generated.Error);

        return await SaveAsync(scope, generated.Value.Curve, accuracies, token);
    }

    private async Task<bool> SaveAsync(Scope scope, CustomCurve? curve, double[] benchmarks, CancellationToken token)
    {
        if (curve is null)
        {
            var removed = await db.RankedMapPointCurves
                .Where(x => x.RankedMapId == scope.Map.Id && x.PointId == scope.Point.Id)
                .ExecuteDeleteAsync(token);

            db.ChangeTracker.Clear();

            return removed > 0;
        }

        var samples = Array.ConvertAll(curve.Points, x => new NpgsqlPoint(x.X, x.Y));
        var updatedAt = clock.GetUtcNow();

        // PostgreSQL point[] has no equality operator, we compare its ordered double coordinates exactly.
        var changed = await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO "RankedMapPointCurves" AS current
                 ("RankedMapId", "PointId", "Curve", "BenchmarkAccuracies", "UpdatedAt")
             VALUES ({scope.Map.Id.Value}, {scope.Point.Id.Value}, {samples}, {benchmarks}, {updatedAt})
             ON CONFLICT ("RankedMapId", "PointId") DO UPDATE
             SET "Curve" = EXCLUDED."Curve",
                 "BenchmarkAccuracies" = EXCLUDED."BenchmarkAccuracies",
                 "UpdatedAt" = EXCLUDED."UpdatedAt"
             WHERE ARRAY(
                 SELECT ARRAY[sample[0], sample[1]] FROM unnest(current."Curve") AS sample
             ) IS DISTINCT FROM ARRAY(
                 SELECT ARRAY[sample[0], sample[1]] FROM unnest(EXCLUDED."Curve") AS sample
             )
             """, token) > 0;

        if (!changed)
            await db.Database.ExecuteSqlAsync(
                $"""
                 UPDATE "RankedMapPointCurves"
                 SET "BenchmarkAccuracies" = {benchmarks}, "UpdatedAt" = {updatedAt}
                 WHERE "RankedMapId" = {scope.Map.Id.Value} AND "PointId" = {scope.Point.Id.Value}
                     AND ARRAY(
                         SELECT ARRAY[sample[0], sample[1]] FROM unnest("Curve") AS sample
                     ) IS NOT DISTINCT FROM ARRAY(
                         SELECT ARRAY[sample[0], sample[1]] FROM unnest({samples}) AS sample
                     )
                 """, token);

        db.ChangeTracker.Clear();

        return changed;
    }

    internal async Task<Scope?> LoadAsync(
        ContextId contextId, RankedMapId mapId, Point.PointId pointId, CancellationToken token)
    {
        var map = await db.RankedMaps
            .SingleOrDefaultAsync(x =>
                x.Id == mapId
                && x.ContextId == contextId
                && x.Context.Points.Any(p => p.Id == pointId && p.GuildId == x.GuildId), token);
        if (map is null) return null;

        var point = await db.Points
            .SingleAsync(x => x.Id == pointId && x.GuildId == map.GuildId, token);

        var curve = point.CurveSettings.MapCurvePolicy == PointCurvePolicy.Shared
            ? null
            : await db.RankedMapPointCurves
                .SingleOrDefaultAsync(x => x.RankedMapId == mapId && x.PointId == pointId, token);

        return new Scope(point, map, curve);
    }

    private static PointCurveRequests.Sample[] Samples(CustomCurve curve)
        => Array.ConvertAll(curve.Points, x => new PointCurveRequests.Sample(x.X, x.Y));

    private static PointCurveResponses.Curve Response(Scope scope) => new(
        Samples(scope.Override?.Curve ?? scope.Point.CurveSettings.Accuracy),
        scope.Override is not null,
        scope.Point.CurveSettings.MapCurvePolicy == PointCurvePolicy.EditableMapCurve,
        scope.Override?.UpdatedAt,
        scope.Override?.BenchmarkAccuracies ?? []);

    private static PointCurveResponses.Settings Settings(CurveSettings settings) => new(
        Samples(settings.Difficulty),
        Samples(settings.Accuracy),
        settings.AccuracyBonusRatio,
        settings.MapCurvePolicy switch
        {
            PointCurvePolicy.Shared => new PointCurveRequests.Policy.Shared(),
            PointCurvePolicy.EditableMapCurve => new PointCurveRequests.Policy.EditableMapCurve(),
            _ => new PointCurveRequests.Policy.ScoreDerivedMapCurve(settings.RunnerUpRewardFractions)
        });

    private static Result<CurveSettings> ValidateSettings(PointCurveRequests.UpdateSettings request)
    {
        if (request.Policy is not (PointCurveRequests.Policy.Shared or PointCurveRequests.Policy.EditableMapCurve
                                                                    or PointCurveRequests.Policy.ScoreDerivedMapCurve)
            || request.DifficultyCurve is not { Length: >= 2 and <= AccuracyCurveMath.MaximumPoints })
            return Failure<CurveSettings>("A policy and 2..256 difficulty samples are required.");

        var difficulty = request.DifficultyCurve
            .Select(x => new CurvePoint(x.X, x.Y))
            .ToArray();

        if (difficulty[0].X < difficulty[^1].X) Array.Reverse(difficulty);

        if (difficulty
            .Where((sample, index) =>
                !double.IsFinite(sample.X)
                || sample.X < 0
                || index > 0 && difficulty[index - 1].X <= sample.X
                || AccuracyCurveMath.MaximumReward(sample.Y, request.AccuracyBonusRatio).IsFailure)
            .Any())
            return Failure<CurveSettings>(
                "Difficulty samples must be ordered, finite and yield representable nonnegative rewards.");

        if (request.DefaultAccuracyCurve is not { Length: <= AccuracyCurveMath.MaximumPoints })
            return Failure<CurveSettings>("An accuracy curve requires 2..256 samples.");

        var native = request.Policy as PointCurveRequests.Policy.ScoreDerivedMapCurve;
        var accuracy = request is { AccuracyBonusRatio: 0, DefaultAccuracyCurve: [] }
            ? Success(new CustomCurve([]))
            : AccuracyCurveMath.Validate(
                Array.ConvertAll(request.DefaultAccuracyCurve, x => new CurvePoint(x.X, x.Y)),
                requireMonotone: native is not null);

        if (accuracy.IsFailure)
            return Failure<CurveSettings>(accuracy.Error);

        var fractions = native?.RunnerUpRewardFractions ?? [];
        if (native is not null
            && (native.RunnerUpRewardFractions is not { Length: <= NativeCurveGenerator.MaximumRunnerUps }
                || NativeCurveGenerator.Generate([1], fractions, SamplingTolerance).IsFailure))
            return Failure<CurveSettings>(
                "Runner-up fractions must contain at most ten non-increasing values in [0,1].");

        return new CurveSettings
        {
            Difficulty = new CustomCurve(difficulty),
            Accuracy = accuracy.Value,
            AccuracyBonusRatio = request.AccuracyBonusRatio,
            MapCurvePolicy = request.Policy switch
            {
                PointCurveRequests.Policy.Shared => PointCurvePolicy.Shared,
                PointCurveRequests.Policy.EditableMapCurve => PointCurvePolicy.EditableMapCurve,
                _ => PointCurvePolicy.ScoreDerivedMapCurve
            },
            RunnerUpRewardFractions = (double[])fractions.Clone()
        };
    }
}