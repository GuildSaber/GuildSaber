using System.Runtime.CompilerServices;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedScores;

public sealed class RankedScoreService(
    ServerDbContext dbContext,
    TimeProvider timeProvider,
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory)
{
    private static readonly string _updateRankedScoreConfirmationStateFormattableString =
        $$"""
          UPDATE "{{nameof(ServerDbContext.RankedScores)}}"
          SET "{{nameof(RankedScore.Type)}}" = {0},
              "{{nameof(PointGivingRankedScore.Rank)}}" = CASE WHEN {0} = {{(int)RankedScore.ERankedScoreType.Accepted}} THEN 0 ELSE NULL END,
              "{{nameof(RankedScore.EditedAt)}}" = {1}
          WHERE "{{nameof(RankedScore.ScoreId)}}" = {2}
              AND "{{nameof(RankedScore.ContextId)}}" = {3}
              AND "{{nameof(RankedScore.Type)}}" IN (
                  {{(int)RankedScore.ERankedScoreType.Pending}},
                  {{(int)RankedScore.ERankedScoreType.Accepted}},
                  {{(int)RankedScore.ERankedScoreType.Refused}}
              )
          """;

    public abstract record ConfirmationResponse
    {
        public sealed record Success(RankedScore[] RankedScores) : ConfirmationResponse;
        public sealed record NotFound : ConfirmationResponse;
    }

    internal static Task<int> UpdateRankedScoreConfirmationStateAsync(
        ServerDbContext dbContext,
        TimeProvider timeProvider,
        ContextId contextId,
        ScoreId scoreId,
        RankedScore.ERankedScoreType targetType,
        CancellationToken token)
        => dbContext.Database.ExecuteSqlAsync(
            FormattableStringFactory.Create(
                _updateRankedScoreConfirmationStateFormattableString,
                (int)targetType,
                timeProvider.GetUtcNow(),
                scoreId.Value,
                contextId.Value),
            token);

    public Task<ConfirmationResponse> SetConfirmedAsync(
        ContextId contextId, ScoreId scoreId, CancellationToken token)
        => SetConfirmationStateAsync(contextId, scoreId, RankedScore.ERankedScoreType.Accepted, token);

    public Task<ConfirmationResponse> SetDeniedAsync(
        ContextId contextId, ScoreId scoreId, CancellationToken token)
        => SetConfirmationStateAsync(contextId, scoreId, RankedScore.ERankedScoreType.Refused, token);

    public Task<ConfirmationResponse> RevertToPendingAsync(
        ContextId contextId, ScoreId scoreId, CancellationToken token)
        => SetConfirmationStateAsync(contextId, scoreId, RankedScore.ERankedScoreType.Pending, token);

    private async Task<ConfirmationResponse> SetConfirmationStateAsync(
        ContextId contextId,
        ScoreId scoreId,
        RankedScore.ERankedScoreType targetType,
        CancellationToken token)
    {
        var updatedCount = await UpdateRankedScoreConfirmationStateAsync(
            dbContext,
            timeProvider,
            contextId,
            scoreId,
            targetType,
            token
        );

        if (updatedCount == 0)
            return new ConfirmationResponse.NotFound();

        dbContext.ChangeTracker.Clear();
        var rankedScores = await dbContext.RankedScores
            .Include(x => x.Score)
            .Include(x => x.PrevScore)
            .Where(x => x.ContextId == contextId && x.ScoreId == scoreId)
            .ToArrayAsync(token);

        var ids = rankedScores.Select(x => x.Id).ToArray();
        await taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            foreach (var id in ids)
                await scope.ServiceProvider.GetRequiredService<RankedScoreConfirmationPipeline>()
                    .ExecuteAsync(contextId, id, cancellationToken);
        });

        return new ConfirmationResponse.Success(rankedScores);
    }
}