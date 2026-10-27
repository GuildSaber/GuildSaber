using System.Data;
using System.Runtime.CompilerServices;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedMaps.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GuildSaber.Api.Features.MapRequests.Services;

public sealed class MapProposalReviewService(
    ServerDbContext dbContext,
    MapProposalService proposalService,
    RankedMapLimitService rankedMapService,
    TimeProvider timeProvider,
    IOptions<MapProposalReviewSettings> settings,
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory)
{
    private static readonly string _reviewProposalFormattableString =
        $$"""
          UPDATE "{{nameof(ServerDbContext.MapProposals)}}"
          SET "status" = {0},
              "{{nameof(AcceptedProposal.AcceptedAt)}}" = CASE WHEN {0} = {{(byte)MapProposalStatus.Accepted}} THEN {1} ELSE NULL END,
              "{{nameof(AcceptedProposal.AcceptedById)}}" = CASE WHEN {0} = {{(byte)MapProposalStatus.Accepted}} THEN {2}::integer ELSE NULL END,
              "{{nameof(ClosedProposal.ClosedAt)}}" = CASE WHEN {0} = {{(byte)MapProposalStatus.Closed}} THEN {1} ELSE NULL END,
              "{{nameof(ClosedProposal.ClosedById)}}" = CASE WHEN {0} = {{(byte)MapProposalStatus.Closed}} THEN {2}::integer ELSE NULL END,
              "{{nameof(ClosedProposal.Reason)}}" = {3}
          WHERE "{{nameof(MapProposal.Id)}}" = {4}
              AND "{{nameof(MapProposal.ThreadId)}}" = {5}
              AND "status" = {{(byte)MapProposalStatus.Submitted}}
          """;

    private static readonly string _closeAlternativesFormattableString =
        $$"""
          UPDATE "{{nameof(ServerDbContext.MapProposals)}}"
          SET "status" = {{(byte)MapProposalStatus.Closed}},
              "{{nameof(ClosedProposal.ClosedAt)}}" = {0},
              "{{nameof(ClosedProposal.ClosedById)}}" = {1},
              "{{nameof(ClosedProposal.Reason)}}" = {2}
          WHERE "{{nameof(MapProposal.ThreadId)}}" = {3}
              AND "status" = {{(byte)MapProposalStatus.Submitted}}
          """;

    public closed record ReviewResponse
    {
        public sealed record Accepted(
            AcceptedProposal Proposal,
            RankedMapId RankedMapId,
            SongDifficultyId[] AddedDifficultyIds) : ReviewResponse;

        public sealed record Denied(ClosedProposal Proposal) : ReviewResponse;
        public sealed record NotFound : ReviewResponse;
        public sealed record InvalidState : ReviewResponse;
        public sealed record Conflict : ReviewResponse;

        public sealed record VoteRequirementsNotMet(
            int PositiveVotes,
            int NegativeVotes,
            int RequiredPositiveVotes,
            bool RequireNoNegativeVotes) : ReviewResponse;

        public sealed record TooManyRankedMaps(int CurrentCount, int MaximumCount) : ReviewResponse;
        public sealed record ValidationFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : ReviewResponse;
    }

    public async Task<ReviewResponse> AcceptAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, CancellationToken token,
        bool bypassVoteRequirements = false)
    {
        var response = await ReviewAsync(contextId, proposalId, playerId, MapProposalStatus.Accepted, null, token,
            bypassVoteRequirements);
        if (response is ReviewResponse.Accepted { RankedMapId: var mapId, AddedDifficultyIds: var addedDifficulties })
            await taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ApplyMapProposalPipeline>()
                    .ExecuteAsync(mapId, addedDifficulties, cancellationToken);
            });
        return response;
    }

    public Task<ReviewResponse> DenyAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, string? reason, CancellationToken token)
        => ReviewAsync(contextId, proposalId, playerId, MapProposalStatus.Closed, reason, token);

    // Trusted import applies refreshes inline. Enqueuing from the import worker could fill its own bounded queue.
    internal Task<ReviewResponse> AcceptImportedAsync(
        ContextId contextId, MapProposalId proposalId, CancellationToken token)
        => ReviewAsync(contextId, proposalId, null, MapProposalStatus.Accepted, null, token,
            bypassVoteRequirements: true);

    private Task<ReviewResponse> ReviewAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId? playerId, MapProposalStatus status, string? reason,
        CancellationToken token, bool bypassVoteRequirements = false)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, proposalId, playerId, status, reason, bypassVoteRequirements),
            static (state, cancellationToken) => state.service.ReviewCoreAsync(
                state.contextId, state.proposalId, state.playerId, state.status, state.reason,
                state.bypassVoteRequirements, cancellationToken),
            token);

    private async Task<ReviewResponse> ReviewCoreAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId? playerId, MapProposalStatus status, string? reason,
        bool bypassVoteRequirements, CancellationToken token)
    {
        if (dbContext.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Save or discard pending changes before reviewing a proposal.");

        // Load a fresh writable graph. The operation owns its tracked changes, including rollback cleanup.
        dbContext.ChangeTracker.Clear();
        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, token);
            var proposal = await GetProposal(contextId, proposalId).SingleOrDefaultAsync(token);
            if (proposal is null)
                return new ReviewResponse.NotFound();

            var guildId = await dbContext.Contexts.Where(x => x.Id == contextId).Select(x => x.GuildId)
                .SingleAsync(token);
            if (playerId is null && (proposal.AuthorId is not null || !await dbContext.MapRequestThreads
                    .AnyAsync(x => x.Id == proposal.ThreadId && x.IsLegacyImport, token)))
                return new ReviewResponse.InvalidState();
            if (proposal is not SubmittedProposal submitted)
                return new ReviewResponse.InvalidState();

            var now = timeProvider.GetUtcNow();
            RankedMap? map = null;
            SongDifficultyId[] addedDifficulties = [];
            if (status == MapProposalStatus.Accepted)
            {
                if (!bypassVoteRequirements)
                {
                    var votes = await dbContext.MapProposalVotes.OfType<RankingTeamProposalVote>()
                        .Where(x => x.ProposalId == proposalId)
                        .GroupBy(x => x.ProposalId)
                        .Select(x => new
                        {
                            Positive = x.Count(vote => vote.IsPositive),
                            Negative = x.Count(vote => !vote.IsPositive)
                        })
                        .SingleOrDefaultAsync(token);
                    if ((votes?.Positive ?? 0) < settings.Value.MinimumPositiveVotes
                        || settings.Value.RequireNoNegativeVotes && votes?.Negative > 0)
                        return new ReviewResponse.VoteRequirementsNotMet(votes?.Positive ?? 0, votes?.Negative ?? 0,
                            settings.Value.MinimumPositiveVotes, settings.Value.RequireNoNegativeVotes);
                }

                var errors = await proposalService.ValidateAsync(contextId, submitted, token);
                if (submitted.Rating.AccStar is null || submitted.Rating.DiffStar is null)
                    errors.Add(new KeyValuePair<string, string[]>(nameof(MapProposal.Rating),
                        ["Ratings must be resolved before acceptance."]));
                if (errors.Count > 0)
                    return new ReviewResponse.ValidationFailure(errors);

                map = await dbContext.RankedMaps.AsTracking().Where(x => x.ThreadId == proposal.ThreadId)
                    .Include(x => x.MapVersions).Include(x => x.Categories).Include(x => x.Achievements)
                    .AsSplitQuery().SingleOrDefaultAsync(token);
                if (map is not null && (map.ContextId != contextId || map.GuildId != guildId))
                    return new ReviewResponse.ValidationFailure([
                        new KeyValuePair<string, string[]>(nameof(MapRequestThread.ContextId),
                            ["The ranked map and thread must belong to the same context."])
                    ]);
                if (map is null)
                    // The creation limit does not apply when updating the thread's existing ranked map.
                    if (rankedMapService.ValidateRankedMapCreationLimit(
                            await rankedMapService.GetCurrentGuildRankedMapCount(guildId),
                            await rankedMapService.GetBoostsCountsAsync(guildId)).TryGetError(out var limit))
                        return new ReviewResponse.TooManyRankedMaps(limit.current, limit.max);

                var latestAcceptance = await dbContext.MapProposals.OfType<AcceptedProposal>()
                    .Where(x => x.ThreadId == proposal.ThreadId).Select(x => (DateTimeOffset?)x.AcceptedAt)
                    .MaxAsync(token);
                // PostgreSQL timestamps have microsecond precision. The latest accepted snapshot must stay unambiguous.
                if (latestAcceptance is { } previous && now < previous.AddMicroseconds(1))
                    now = previous.AddMicroseconds(1);

                var previousDifficulties = map?.MapVersions.Select(x => x.SongDifficultyId).Distinct().ToArray() ?? [];
                map = await ApplyAsync(map, guildId, contextId, submitted, now, token);
                addedDifficulties = submitted.MapVersions.Select(x => x.SongDifficultyId).Distinct()
                    .Except(previousDifficulties).ToArray();
            }

            if (await dbContext.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
                    _reviewProposalFormattableString, (byte)status, now, playerId?.Value!, reason!, proposalId.Value,
                    proposal.ThreadId.Value), token) == 0)
                return new ReviewResponse.Conflict();

            if (status == MapProposalStatus.Accepted)
                await dbContext.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
                    _closeAlternativesFormattableString, now, playerId?.Value!, "Another proposal was accepted.",
                    proposal.ThreadId.Value), token);

            if (status == MapProposalStatus.Accepted || !await dbContext.MapProposals.OfType<SubmittedProposal>()
                    .AnyAsync(x => x.ThreadId == proposal.ThreadId, token))
                await dbContext.MapRequestThreadBoosts.Where(x => x.ThreadId == proposal.ThreadId)
                    .ExecuteDeleteAsync(token);

            await dbContext.SaveChangesAsync(token);
            if (map is not null)
                await dbContext.RankedScores.Where(score => score.RankedMapId == map.Id
                                                            && !dbContext.MapVersions.Any(version
                                                                => version.RankedMapId == map.Id
                                                                   && version.SongDifficultyId ==
                                                                   score.SongDifficultyId)).ExecuteDeleteAsync(token);

            var updated = await GetProposal(contextId, proposalId).SingleAsync(token);
            await transaction.CommitAsync(token);
            return updated switch
            {
                AcceptedProposal accepted => new ReviewResponse.Accepted(accepted, map!.Id, addedDifficulties),
                ClosedProposal closed => new ReviewResponse.Denied(closed),
                _ => throw new InvalidOperationException(
                    "The proposal did not transition to the requested review state.")
            };
        }
        catch (Exception exception) when ((exception as PostgresException ??
                                           exception.InnerException as PostgresException)
                                          ?.SqlState is PostgresErrorCodes.SerializationFailure
                                                        or PostgresErrorCodes.DeadlockDetected
                                                        or PostgresErrorCodes.UniqueViolation
                                                        or PostgresErrorCodes.ForeignKeyViolation)
        {
            return new ReviewResponse.Conflict();
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    private IQueryable<MapProposal> GetProposal(ContextId contextId, MapProposalId proposalId)
        => dbContext.MapProposals.Where(x => x.Id == proposalId && x.Thread.ContextId == contextId)
            .Include(x => x.MapVersions).Include(x => x.Categories).Include(x => x.Achievements).AsSplitQuery();

    private async Task<RankedMap> ApplyAsync(
        RankedMap? map, GuildId guildId, ContextId contextId, SubmittedProposal proposal, DateTimeOffset now,
        CancellationToken token)
    {
        var categoryIds = proposal.Categories.Select(x => x.CategoryId);
        var achievementIds = proposal.Achievements.Select(x => x.AchievementId);
        var difficultyIds = proposal.MapVersions.Select(x => x.SongDifficultyId).Distinct();
        var songs = await dbContext.SongDifficulties
            .Where(x => difficultyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.SongId, token);

        if (map is null)
        {
            map = new RankedMap
            {
                GuildId = guildId,
                ContextId = contextId,
                ThreadId = proposal.ThreadId,
                Info = new RankedMapInfo(now, now),
                Requirements = proposal.Requirements,
                Rating = new RankedMapRating
                {
                    AccStar = proposal.Rating.AccStar!.Value,
                    DiffStar = proposal.Rating.DiffStar!.Value
                },
                MapVersions = [],
                Categories = [],
                Achievements = []
            };
            dbContext.RankedMaps.Add(map);
        }
        else
        {
            map.Info = map.Info with { EditedAt = now };
            map.Requirements = proposal.Requirements;
            map.Rating.AccStar = proposal.Rating.AccStar!.Value;
            map.Rating.DiffStar = proposal.Rating.DiffStar!.Value;
        }

        var proposedVersions = proposal.MapVersions.ToDictionary(x => (x.SongDifficultyId, x.PlayModeId));
        for (var index = map.MapVersions.Count - 1; index >= 0; index--)
        {
            var version = map.MapVersions[index];
            if (proposedVersions.Remove((version.SongDifficultyId, version.PlayModeId), out var replacement))
            {
                version.Order = replacement.Order; // Keep AddedAt for versions already present.
            }
            else
            {
                map.MapVersions.RemoveAt(index);
                dbContext.MapVersions.Remove(version);
            }
        }

        foreach (var version in proposedVersions.Values)
            map.MapVersions.Add(new MapVersion
            {
                SongDifficultyId = version.SongDifficultyId,
                SongId = songs[version.SongDifficultyId],
                PlayModeId = version.PlayModeId,
                Order = version.Order,
                AddedAt = now
            });

        map.Categories.Clear();
        await foreach (var category in dbContext.Categories.AsTracking()
                           .Where(x => categoryIds.Contains(x.Id))
                           .AsAsyncEnumerable()
                           .WithCancellation(token))
            map.Categories.Add(category);

        map.Achievements.Clear();
        await foreach (var achievement in dbContext.Achievements.OfType<RankedMapListAchievement>().AsTracking()
                           .Where(x => achievementIds.Contains(x.Id))
                           .AsAsyncEnumerable()
                           .WithCancellation(token))
            map.Achievements.Add(achievement);

        return map;
    }
}