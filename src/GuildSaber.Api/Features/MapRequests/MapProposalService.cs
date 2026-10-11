using System.Data;
using System.Runtime.CompilerServices;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using AchievementId = GuildSaber.Database.Models.Server.Guilds.Achievements.Achievement.AchievementId;
using PlayModeId = GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes.PlayMode.PlayModeId;

namespace GuildSaber.Api.Features.MapRequests;

public sealed class MapProposalService(ServerDbContext dbContext, TimeProvider timeProvider)
{
    private static readonly string _updateProposalStateFormattableString =
        $$"""
          UPDATE "{{nameof(ServerDbContext.MapProposals)}}" AS proposal
          SET "status" = {0},
              "{{nameof(SubmittedProposal.SubmittedAt)}}" = CASE
                  WHEN {0} = {{(byte)MapProposalStatus.Submitted}} THEN {1}
                  ELSE proposal."{{nameof(SubmittedProposal.SubmittedAt)}}" END,
              "{{nameof(ClosedProposal.ClosedAt)}}" = CASE
                  WHEN {0} = {{(byte)MapProposalStatus.Closed}} THEN {1} ELSE NULL END,
              "{{nameof(ClosedProposal.ClosedById)}}" = CASE
                  WHEN {0} = {{(byte)MapProposalStatus.Closed}} THEN {2} ELSE NULL END,
              "{{nameof(ClosedProposal.Reason)}}" = {3}
          WHERE proposal."{{nameof(MapProposal.Id)}}" = {4}
              AND proposal."{{nameof(MapProposal.AuthorId)}}" = {2}
              AND proposal."status" = {5}
              AND EXISTS (
                  SELECT 1 FROM "{{nameof(ServerDbContext.MapRequestThreads)}}" AS thread
                  WHERE thread."{{nameof(MapRequestThread.Id)}}" = proposal."{{nameof(MapProposal.ThreadId)}}"
                      AND thread."{{nameof(MapRequestThread.ContextId)}}" = {6})
          """;

    public closed record TransitionResponse
    {
        public sealed record Success(MapProposal Proposal) : TransitionResponse;
        public sealed record NotFound : TransitionResponse;
        public sealed record Forbidden : TransitionResponse;
        public sealed record InvalidState : TransitionResponse;
        public sealed record Conflict : TransitionResponse;
        public sealed record ValidationFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : TransitionResponse;
    }

    public Task<TransitionResponse> SubmitAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, proposalId, playerId),
            static (state, cancellationToken) => state.service.SetStateAsync(
                state.contextId, state.proposalId, state.playerId, MapProposalStatus.Submitted, null, cancellationToken),
            token);

    public Task<TransitionResponse> CloseAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, string? reason, CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, proposalId, playerId, reason),
            static (state, cancellationToken) => state.service.SetStateAsync(
                state.contextId, state.proposalId, state.playerId, MapProposalStatus.Closed, state.reason, cancellationToken),
            token);

    private async Task<TransitionResponse> SetStateAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId,
        MapProposalStatus targetStatus, string? reason, CancellationToken token)
    {
        try
        {
            // Validate and transition against one consistent view of the proposal and its references.
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var proposal = await GetProposal(contextId, proposalId).SingleOrDefaultAsync(token);
            if (proposal is null)
                return new TransitionResponse.NotFound();

            if (GetTransitionError(proposal, playerId, targetStatus) is { } error)
                return error;

            var trackedConfiguration = dbContext.ChangeTracker.Entries().Where(entry => entry.Entity switch
            {
                MapProposal tracked => tracked.Id == proposalId,
                MapProposalVersion version => version.ProposalId == proposalId,
                MapProposalCategory category => category.ProposalId == proposalId,
                MapProposalAchievement achievement => achievement.ProposalId == proposalId,
                _ => false
            }).ToArray();
            if (trackedConfiguration.Any(entry
                    => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException(
                    "Save or discard pending proposal changes before transitioning it.");

            if (proposal is DraftProposal draft)
            {
                var errors = await ValidateAsync(contextId, draft, token);
                if (errors.Count > 0)
                    return new TransitionResponse.ValidationFailure(errors);
            }

            var expectedStatus = proposal is DraftProposal ? MapProposalStatus.Draft : MapProposalStatus.Submitted;
            var updatedCount = await dbContext.Database.ExecuteSqlAsync(
                FormattableStringFactory.Create(
                    _updateProposalStateFormattableString,
                    (byte)targetStatus,
                    timeProvider.GetUtcNow(),
                    playerId.Value,
                    reason!,
                    proposalId.Value,
                    (byte)expectedStatus,
                    contextId.Value),
                token);

            if (updatedCount == 0)
                return new TransitionResponse.Conflict();

            // Materialize the new CLR subtype; the old instance cannot change its runtime type.
            var updated = await GetProposal(contextId, proposalId).SingleAsync(token);
            await transaction.CommitAsync(token);

            // Invalidate the old subtype and its snapshots, leaving unrelated tracked entities alone.
            foreach (var entry in trackedConfiguration)
                entry.State = EntityState.Detached;

            return new TransitionResponse.Success(updated);
        }
        catch (PostgresException exception) when (exception.SqlState is
                                                      PostgresErrorCodes.SerializationFailure
                                                      or PostgresErrorCodes.DeadlockDetected)
        {
            return new TransitionResponse.Conflict();
        }
    }

    private IQueryable<MapProposal> GetProposal(ContextId contextId, MapProposalId proposalId)
        => dbContext.MapProposals
            .Where(x => x.Id == proposalId && x.Thread.ContextId == contextId)
            .Include(x => x.MapVersions)
            .Include(x => x.Categories)
            .Include(x => x.Achievements)
            .AsSplitQuery();

    internal static TransitionResponse? GetTransitionError(
        MapProposal proposal, PlayerId playerId, MapProposalStatus targetStatus)
        => proposal.AuthorId != playerId
            ? new TransitionResponse.Forbidden()
            : (proposal, targetStatus) switch
            {
                (DraftProposal, MapProposalStatus.Submitted) => null,
                (SubmittedProposal, MapProposalStatus.Closed) => null,
                _ => new TransitionResponse.InvalidState()
            };

    private async Task<List<KeyValuePair<string, string[]>>> ValidateAsync(
        ContextId contextId, DraftProposal proposal, CancellationToken token)
    {
        var errors = GetConfigurationErrors(proposal);
        var guildId = await dbContext.Contexts.Where(x => x.Id == contextId)
            .Select(x => x.GuildId).SingleAsync(token);

        var categoryIds = proposal.Categories.Select(x => x.CategoryId).ToArray();
        var categories = await dbContext.Categories
            .Where(x => ((IEnumerable<CategoryId>)categoryIds).Contains(x.Id) && x.GuildId == guildId)
            .Select(x => x.Id).ToArrayAsync(token);
        var categoryErrors = categoryIds.Except(categories)
            .Select(id => $"Category with ID '{id}' does not exist in the guild.").ToArray();
        if (categoryErrors.Length > 0)
            errors.Add(new KeyValuePair<string, string[]>(nameof(MapProposal.Categories), categoryErrors));

        var achievementIds = proposal.Achievements.Select(x => x.AchievementId).ToArray();
        var achievements = await dbContext.Achievements.OfType<RankedMapListAchievement>()
            .Where(x => ((IEnumerable<AchievementId>)achievementIds).Contains(x.Id)
                        && x.GuildId == guildId && x.ContextId == contextId)
            .Select(x => x.Id).ToArrayAsync(token);
        var achievementErrors = achievementIds.Except(achievements)
            .Select(id
                => $"Achievement with ID '{id}' does not exist in the context or is not a ranked-map-list achievement.")
            .ToArray();
        if (achievementErrors.Length > 0)
            errors.Add(new KeyValuePair<string, string[]>(nameof(MapProposal.Achievements), achievementErrors));

        var playModeIds = proposal.MapVersions.Select(x => x.PlayModeId).Distinct().ToArray();
        var playModes = await dbContext.PlayModes.Where(x => ((IEnumerable<PlayModeId>)playModeIds).Contains(x.Id))
            .Select(x => x.Id).ToArrayAsync(token);
        var playModeErrors = playModeIds.Except(playModes)
            .Select(id => $"Play mode with ID '{id}' no longer exists.").ToArray();
        if (playModeErrors.Length > 0)
            errors.Add(new KeyValuePair<string, string[]>(nameof(MapProposal.MapVersions), playModeErrors));

        return errors;
    }

    internal static List<KeyValuePair<string, string[]>> GetConfigurationErrors(DraftProposal proposal)
    {
        List<KeyValuePair<string, string[]>> errors = [];

        if (proposal.MapVersions.Count == 0)
            errors.Add(new KeyValuePair<string, string[]>(nameof(MapProposal.MapVersions),
                ["A proposal must contain at least one map version."]));

        if (!float.IsFinite(proposal.Rating.AccStar) || proposal.Rating.AccStar < 0)
            errors.Add(new KeyValuePair<string, string[]>("Rating.AccStar",
                ["Accuracy stars must be finite and non-negative."]));
        if (!float.IsFinite(proposal.Rating.DiffStar) || proposal.Rating.DiffStar < 0)
            errors.Add(new KeyValuePair<string, string[]>("Rating.DiffStar",
                ["Difficulty stars must be finite and non-negative."]));

        var requirements = proposal.Requirements;
        if (requirements.MaxPauseDurationSec is { } pause && (!float.IsFinite(pause) || pause < 0))
            errors.Add(new KeyValuePair<string, string[]>("Requirements.MaxPauseDurationSec",
                ["Maximum pause duration must be finite and non-negative."]));
        if (requirements.MinAccuracy is { } accuracy && (!float.IsFinite(accuracy) || accuracy < 0 || accuracy > 100))
            errors.Add(new KeyValuePair<string, string[]>("Requirements.MinAccuracy",
                ["Minimum accuracy must be finite and between 0 and 100."]));
        if ((requirements.MandatoryModifiers & requirements.ProhibitedModifiers) != 0)
            errors.Add(new KeyValuePair<string, string[]>("Requirements.MandatoryModifiers",
                ["A modifier cannot be both mandatory and prohibited."]));

        return errors;
    }
}
