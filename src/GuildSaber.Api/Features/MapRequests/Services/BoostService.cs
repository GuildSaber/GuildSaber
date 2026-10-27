using System.Data;
using System.Runtime.CompilerServices;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GuildSaber.Api.Features.MapRequests.Services;

public sealed class MapRequestBoostService(ServerDbContext dbContext, IOptions<MapRequestBoostSettings> settings)
{
    private static readonly string _insertBoostFormattableString =
        $$"""
          INSERT INTO "{{nameof(ServerDbContext.MapRequestThreadBoosts)}}"
              ("{{nameof(MapRequestThreadBoost.PlayerId)}}", "{{nameof(MapRequestThreadBoost.ThreadId)}}")
          VALUES ({0}, {1})
          ON CONFLICT DO NOTHING
          """;

    public closed record BoostResponse
    {
        public sealed record Success : BoostResponse;
        public sealed record PlayerNotFound : BoostResponse;
        public sealed record ThreadNotFound : BoostResponse;
        public sealed record SourceNotFound : BoostResponse;
        public sealed record NoSubmittedProposals : BoostResponse;
        public sealed record AlreadyBoosted : BoostResponse;
        public sealed record LimitReached(int Limit) : BoostResponse;
        public sealed record Conflict : BoostResponse;
    }

    public Task<BoostResponse> AddAsync(
        ContextId contextId, MapRequestThreadId threadId, PlayerId playerId, CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, threadId, playerId),
            static (state, cancellationToken) => state.service
                .SetAsync(state.contextId, state.threadId, state.playerId, null, cancellationToken), token);

    /// <summary>Moves an owned allocation to a thread in the target context without consuming another slot.</summary>
    public Task<BoostResponse> MoveAsync(
        ContextId targetContextId, MapRequestThreadId sourceThreadId, MapRequestThreadId targetThreadId,
        PlayerId playerId, CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, targetContextId, sourceThreadId, targetThreadId, playerId),
            static (state, cancellationToken) => state.service
                .SetAsync(state.targetContextId, state.targetThreadId, state.playerId, state.sourceThreadId,
                    cancellationToken), token);

    private async Task<BoostResponse> SetAsync(
        ContextId contextId, MapRequestThreadId threadId, PlayerId playerId, MapRequestThreadId? sourceThreadId,
        CancellationToken token)
    {
        try
        {
            // Serialize quota checks and assignments against concurrent allocations and proposal closure.
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, token);

            var player = await dbContext.Players
                .Where(x => x.Id == playerId)
                .Select(x => new { x.SubscriptionInfo.Tier })
                .SingleOrDefaultAsync(token);

            if (player is null)
                return new BoostResponse.PlayerNotFound();

            var target = await dbContext.MapRequestThreads.Where(x => x.Id == threadId && x.ContextId == contextId)
                .Select(x => new { HasSubmittedProposals = x.Proposals.OfType<SubmittedProposal>().Any() })
                .SingleOrDefaultAsync(token);

            if (target is null)
                return new BoostResponse.ThreadNotFound();

            if (!target.HasSubmittedProposals)
                return new BoostResponse.NoSubmittedProposals();

            var trackedBoosts = dbContext.ChangeTracker.Entries<MapRequestThreadBoost>()
                .Where(x => x.Entity.PlayerId == playerId)
                .ToArray();

            if (trackedBoosts.Any(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException("Save or discard pending boost changes before allocating a boost.");

            var boosts = dbContext.MapRequestThreadBoosts.Where(x => x.PlayerId == playerId);
            if (await boosts.AnyAsync(x => x.ThreadId == threadId, token))
                return sourceThreadId is null || sourceThreadId == threadId
                    ? new BoostResponse.Success()
                    : new BoostResponse.AlreadyBoosted();

            if (sourceThreadId is { } source && !await boosts.AnyAsync(x => x.ThreadId == source, token))
                return new BoostResponse.SourceNotFound();

            var limit = settings.Value.GetLimit(player.Tier);
            var count = await boosts.CountAsync(token);
            if (sourceThreadId is null ? count >= limit : count > limit)
                return new BoostResponse.LimitReached(limit);

            var updatedCount = sourceThreadId is { } previous
                ? await boosts.Where(x => x.ThreadId == previous)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ThreadId, threadId), token)
                : await dbContext.Database.ExecuteSqlAsync(
                    FormattableStringFactory.Create(_insertBoostFormattableString, playerId.Value, threadId.Value),
                    token);

            if (updatedCount == 0)
                return new BoostResponse.Conflict();

            await transaction.CommitAsync(token);
            foreach (var entry in trackedBoosts.Where(x => x.Entity.ThreadId == sourceThreadId))
                entry.State = EntityState.Detached;

            return new BoostResponse.Success();
        }
        catch (PostgresException exception)
            when (exception.SqlState is PostgresErrorCodes.SerializationFailure
                                        or PostgresErrorCodes.DeadlockDetected
                                        or PostgresErrorCodes.UniqueViolation
                                        or PostgresErrorCodes.ForeignKeyViolation)
        {
            return new BoostResponse.Conflict();
        }
        catch (InvalidOperationException exception)
            when (exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.SerializationFailure
                                or PostgresErrorCodes.DeadlockDetected
                  })
        {
            // EF's non-retrying execution strategy wraps transient failures from bulk operations and queries.
            return new BoostResponse.Conflict();
        }
    }

    public async Task<BoostResponse> RemoveAsync(
        ContextId contextId, MapRequestThreadId threadId, PlayerId playerId, CancellationToken token)
    {
        var trackedBoosts = dbContext.ChangeTracker.Entries<MapRequestThreadBoost>()
            .Where(x => x.Entity.ThreadId == threadId && x.Entity.PlayerId == playerId).ToArray();
        if (trackedBoosts.Any(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Save or discard pending boost changes before removing a boost.");

        if (await dbContext.MapRequestThreadBoosts
                .Where(x => x.ThreadId == threadId && x.PlayerId == playerId && x.Thread.ContextId == contextId)
                .ExecuteDeleteAsync(token) > 0)
            foreach (var entry in trackedBoosts)
                entry.State = EntityState.Detached;

        return new BoostResponse.Success();
    }
}