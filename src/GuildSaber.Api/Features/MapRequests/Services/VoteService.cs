using System.Data;
using System.Runtime.CompilerServices;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GuildSaber.Api.Features.MapRequests.Services;

public sealed class MapProposalVoteService(ServerDbContext dbContext, TimeProvider timeProvider)
{
    private static readonly string _upsertVoteFormattableString =
        $$"""
          INSERT INTO "{{nameof(ServerDbContext.MapProposalVotes)}}"
              ("{{nameof(MapProposalVote.ProposalId)}}", "{{nameof(MapProposalVote.PlayerId)}}", "Kind",
               "{{nameof(MapProposalVote.IsPositive)}}", "{{nameof(MapProposalVote.VotedAt)}}", "{{nameof(RankingTeamProposalVote.Reason)}}")
          VALUES ({0}, {1}, {2}, {3}, {4}, {5})
          ON CONFLICT ("{{nameof(MapProposalVote.ProposalId)}}", "{{nameof(MapProposalVote.PlayerId)}}", "Kind")
          DO UPDATE SET "{{nameof(MapProposalVote.IsPositive)}}" = EXCLUDED."{{nameof(MapProposalVote.IsPositive)}}",
              "{{nameof(MapProposalVote.VotedAt)}}" = EXCLUDED."{{nameof(MapProposalVote.VotedAt)}}",
              "{{nameof(RankingTeamProposalVote.Reason)}}" = EXCLUDED."{{nameof(RankingTeamProposalVote.Reason)}}"
          """;

    public closed record VoteResponse
    {
        public sealed record Success : VoteResponse;
        public sealed record NotFound : VoteResponse;
        public sealed record InvalidState : VoteResponse;
        public sealed record Conflict : VoteResponse;
    }

    public Task<VoteResponse> SetCommunityAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, bool isPositive, CancellationToken token)
        => ChangeAsync<CommunityProposalVote>(contextId, proposalId, playerId, isPositive, null, token);

    public Task<VoteResponse> SetRankingTeamAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, bool isPositive, string? reason,
        CancellationToken token)
        => ChangeAsync<RankingTeamProposalVote>(contextId, proposalId, playerId, isPositive, reason, token);

    public Task<VoteResponse> RemoveCommunityAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, CancellationToken token)
        => ChangeAsync<CommunityProposalVote>(contextId, proposalId, playerId, null, null, token);

    public Task<VoteResponse> RemoveRankingTeamAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, CancellationToken token)
        => ChangeAsync<RankingTeamProposalVote>(contextId, proposalId, playerId, null, null, token);

    private Task<VoteResponse> ChangeAsync<TVote>(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, bool? isPositive, string? reason,
        CancellationToken token) where TVote : MapProposalVote
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, proposalId, playerId, isPositive, reason),
            static (state, cancellationToken) => state.service.ChangeCoreAsync<TVote>(
                state.contextId, state.proposalId, state.playerId, state.isPositive, state.reason, cancellationToken),
            token);

    private async Task<VoteResponse> ChangeCoreAsync<TVote>(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, bool? isPositive, string? reason,
        CancellationToken token) where TVote : MapProposalVote
    {
        if (dbContext.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Save or discard pending changes before voting.");

        try
        {
            // The state read and vote write must serialize with acceptance, which reads votes and closes proposals.
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, token);

            if (await dbContext.MapProposals
                    .Where(x => x.Id == proposalId && x.Thread.ContextId == contextId)
                    .Select(x => (bool?)(x is SubmittedProposal))
                    .SingleOrDefaultAsync(token) is not { } submitted)
                return new VoteResponse.NotFound();

            if (!submitted)
                return new VoteResponse.InvalidState();

            if (isPositive is null)
                await dbContext.MapProposalVotes.OfType<TVote>()
                    .Where(x => x.ProposalId == proposalId && x.PlayerId == playerId)
                    .ExecuteDeleteAsync(token);
            else
                await dbContext.Database.ExecuteSqlAsync(
                    FormattableStringFactory.Create(_upsertVoteFormattableString,
                        proposalId.Value, playerId.Value,
                        dbContext.Model.FindEntityType(typeof(TVote))!.GetDiscriminatorValue()!,
                        isPositive.Value, timeProvider.GetUtcNow(), reason!),
                    token);

            await transaction.CommitAsync(token);
            // Detaching changes the tracker collection being enumerated, so take a snapshot first.
            foreach (var entry in dbContext.ChangeTracker.Entries<TVote>()
                         .Where(x => x.Entity.ProposalId == proposalId && x.Entity.PlayerId == playerId).ToArray())
                entry.State = EntityState.Detached;

            return new VoteResponse.Success();
        }
        catch (Exception exception)
            when ((exception as PostgresException ??
                   exception.InnerException as PostgresException)
                  ?.SqlState is PostgresErrorCodes.SerializationFailure
                                or PostgresErrorCodes.DeadlockDetected
                                or PostgresErrorCodes.UniqueViolation
                                or PostgresErrorCodes.ForeignKeyViolation)
        {
            return new VoteResponse.Conflict();
        }
    }
}