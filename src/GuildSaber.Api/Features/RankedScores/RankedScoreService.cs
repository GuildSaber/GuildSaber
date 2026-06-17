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
          WHERE "{{nameof(RankedScore.Id)}}" = {2}
              AND "{{nameof(RankedScore.ContextId)}}" = {3}
              AND "{{nameof(RankedScore.Type)}}" IN (
                  {{(int)RankedScore.ERankedScoreType.Pending}},
                  {{(int)RankedScore.ERankedScoreType.Accepted}},
                  {{(int)RankedScore.ERankedScoreType.Refused}}
              )
          """;

    internal static Task<int> UpdateRankedScoreConfirmationStateAsync(
        ServerDbContext dbContext,
        TimeProvider timeProvider,
        ContextId contextId,
        RankedScoreId rankedScoreId,
        RankedScore.ERankedScoreType targetType,
        CancellationToken token)
        => dbContext.Database.ExecuteSqlAsync(
            FormattableStringFactory.Create(
                _updateRankedScoreConfirmationStateFormattableString,
                (int)targetType,
                timeProvider.GetUtcNow(),
                rankedScoreId.Value,
                contextId.Value),
            token);

    public abstract record ConfirmationResponse
    {
        public sealed record Success(RankedScore RankedScore) : ConfirmationResponse;
        public sealed record NotFound : ConfirmationResponse;
        public sealed record NotPendingCompatible : ConfirmationResponse;

        /// <summary>
        /// Unlikely case where the scores were reprocessed due to a map requirement change while the confirmation state was being
        /// updated.
        /// </summary>
        public sealed record StateChangedBeforeUpdate : ConfirmationResponse;
    }

    public Task<ConfirmationResponse> SetConfirmedAsync(
        ContextId contextId, RankedScoreId rankedScoreId, CancellationToken token)
        => SetConfirmationStateAsync(contextId, rankedScoreId, RankedScore.ERankedScoreType.Accepted, token);

    public Task<ConfirmationResponse> SetDeniedAsync(
        ContextId contextId, RankedScoreId rankedScoreId, CancellationToken token)
        => SetConfirmationStateAsync(contextId, rankedScoreId, RankedScore.ERankedScoreType.Refused, token);

    public Task<ConfirmationResponse> RevertToPendingAsync(
        ContextId contextId, RankedScoreId rankedScoreId, CancellationToken token)
        => SetConfirmationStateAsync(contextId, rankedScoreId, RankedScore.ERankedScoreType.Pending, token);

    private async Task<ConfirmationResponse> SetConfirmationStateAsync(
        ContextId contextId,
        RankedScoreId rankedScoreId,
        RankedScore.ERankedScoreType targetType,
        CancellationToken token)
    {
        var isPendingCompatible = await dbContext.RankedScores
            .Where(x => x.ContextId == contextId && x.Id == rankedScoreId)
            .Select(x => (bool?)(x is PendingRankedScore || x is AcceptedRankedScore || x is RefusedRankedScore))
            .FirstOrDefaultAsync(token);

        switch (isPendingCompatible)
        {
            case null: return new ConfirmationResponse.NotFound();
            case false: return new ConfirmationResponse.NotPendingCompatible();
        }

        var updatedCount = await UpdateRankedScoreConfirmationStateAsync(
            dbContext,
            timeProvider,
            contextId,
            rankedScoreId,
            targetType,
            token
        );

        if (updatedCount == 0)
            return new ConfirmationResponse.StateChangedBeforeUpdate();

        dbContext.ChangeTracker.Clear();
        var rankedScore = await dbContext.RankedScores
            .Include(x => x.Score)
            .Include(x => x.PrevScore)
            .FirstAsync(x => x.Id == rankedScoreId, token);

        await taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<RankedScoreConfirmationPipeline>()
                .ExecuteAsync(contextId, rankedScoreId, cancellationToken);
        });

        return new ConfirmationResponse.Success(rankedScore);
    }
}