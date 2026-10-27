using System.Data;
using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Features.Scores;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using AchievementId = GuildSaber.Database.Models.Server.Guilds.Achievements.Achievement.AchievementId;
using PlayModeId = GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes.PlayMode.PlayModeId;

namespace GuildSaber.Api.Features.MapRequests.Services;

public sealed class MapProposalService(
    ServerDbContext dbContext,
    TimeProvider timeProvider,
    BeatLeaderApi beatLeaderApi,
    IOptions<MapRequestSettings> settings)
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
                  WHEN {0} = {{(byte)MapProposalStatus.Closed}} THEN {2}::integer ELSE NULL END,
              "{{nameof(ClosedProposal.Reason)}}" = {3},
              "{{nameof(MapProposal.Rating)}}_{{nameof(MapProposalRating.AccStar)}}" = {7},
              "{{nameof(MapProposal.Rating)}}_{{nameof(MapProposalRating.DiffStar)}}" = {8},
              "{{nameof(MapProposal.Rating)}}_{{nameof(MapProposalRating.IsAccStarAutoCalculated)}}" = {9},
              "{{nameof(MapProposal.Rating)}}_{{nameof(MapProposalRating.IsDiffStarAutoCalculated)}}" = {10}
          WHERE proposal."{{nameof(MapProposal.Id)}}" = {4}
              AND proposal."{{nameof(MapProposal.AuthorId)}}" IS NOT DISTINCT FROM {2}
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
        public sealed record AmendmentTierRequired : TransitionResponse;
        public sealed record InvalidState : TransitionResponse;
        public sealed record Conflict : TransitionResponse;
        public sealed record Deleted : TransitionResponse;
        public sealed record LimitReached(int Limit) : TransitionResponse;
        public sealed record Duplicates(MapRequestResponses.DuplicateMatch[] Matches) : TransitionResponse;
        public sealed record RatingCalculationFailure(string Message) : TransitionResponse;
        public sealed record ValidationFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : TransitionResponse;
    }

    public Task<TransitionResponse> SubmitAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, CancellationToken token, bool force = false,
        bool bypassAmendmentRequirement = false)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, proposalId, playerId, force, bypassAmendmentRequirement),
            static (state, cancellationToken) => state.service.SetStateAsync(
                state.contextId, state.proposalId, state.playerId, MapProposalStatus.Submitted, null,
                cancellationToken, state.force, state.bypassAmendmentRequirement),
            token);

    public Task<TransitionResponse> CloseAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, string? reason, CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, proposalId, playerId, reason),
            static (state, cancellationToken) => state.service.SetStateAsync(
                state.contextId, state.proposalId, state.playerId, MapProposalStatus.Closed, state.reason,
                cancellationToken),
            token);

    internal Task<TransitionResponse> SubmitImportedAsync(
        ContextId contextId, MapProposalId proposalId, CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync((service: this, contextId, proposalId),
            static (state, cancellationToken) => state.service.SetStateAsync(state.contextId, state.proposalId,
                null, MapProposalStatus.Submitted, null, cancellationToken, force: true), token);

    private async Task<TransitionResponse> SetStateAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId? playerId, MapProposalStatus targetStatus,
        string? reason, CancellationToken token, bool force = false, bool bypassAmendmentRequirement = false)
    {
        try
        {
            // Validate and transition against one consistent view of the proposal and its references.
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, token);

            var proposal = await GetProposal(contextId, proposalId).SingleOrDefaultAsync(token);
            if (proposal is null)
                return new TransitionResponse.NotFound();

            if (GetTransitionError(proposal, playerId, targetStatus) is { } error)
                return error;

            if (playerId is null &&
                !await dbContext.MapRequestThreads.AnyAsync(x => x.Id == proposal.ThreadId && x.IsLegacyImport, token))
                return new TransitionResponse.Forbidden();

            var trackedConfiguration = dbContext.ChangeTracker.Entries().Where(entry => entry.Entity switch
            {
                MapProposal tracked => tracked.Id == proposalId,
                MapProposalVersion version => version.ProposalId == proposalId,
                MapProposalCategory category => category.ProposalId == proposalId,
                MapProposalAchievement achievement => achievement.ProposalId == proposalId,
                _ => false
            }).ToArray();

            if (trackedConfiguration.Any(entry => entry
                    .State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException(
                    "Save or discard pending proposal changes before transitioning it.");

            var trackedBoosts = dbContext.ChangeTracker.Entries<MapRequestThreadBoost>()
                .Where(x => targetStatus == MapProposalStatus.Closed && x.Entity.ThreadId == proposal.ThreadId)
                .ToArray();
            if (trackedBoosts.Any(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException("Save or discard pending boost changes before closing a proposal.");

            var rating = proposal.Rating;
            if (proposal is DraftProposal draft)
            {
                if (playerId is not null && settings.Value.MinimumAmendmentTier > 0 && !bypassAmendmentRequirement
                    && await dbContext.MapProposals.OfType<SubmittedProposal>()
                        .AnyAsync(x => x.ThreadId == draft.ThreadId && x.AuthorId != playerId, token))
                    return new TransitionResponse.AmendmentTierRequired();

                var revision = await dbContext.RankedMaps.AnyAsync(x => x.ThreadId == draft.ThreadId, token);
                var limit = revision
                    ? settings.Value.MaxRevisionRequestsPerPlayer
                    : settings.Value.MaxRankingRequestsPerPlayer;

                var count = await dbContext.MapProposals.OfType<SubmittedProposal>()
                    .CountAsync(x => x.AuthorId == playerId && x.Thread.ContextId == contextId
                                                            && x.Thread.RankedMap != null == revision, token);
                if (playerId is not null && count >= limit)
                    return new TransitionResponse.LimitReached(limit);

                if (!revision && !force)
                {
                    var duplicates = await FindDuplicatesAsync(contextId, draft, token);
                    if (duplicates.Length > 0)
                        return new TransitionResponse.Duplicates(duplicates);
                }

                var errors = await ValidateAsync(contextId, draft, token);
                if (errors.Count > 0)
                    return new TransitionResponse.ValidationFailure(errors);

                if (!(await ResolveRatingAsync(contextId, draft, token)).TryGetValue(out rating, out var ratingError))
                    return new TransitionResponse.RatingCalculationFailure(ratingError);
            }

            var expectedStatus = proposal is DraftProposal ? MapProposalStatus.Draft : MapProposalStatus.Submitted;
            var updatedCount = await dbContext.Database.ExecuteSqlAsync(
                FormattableStringFactory.Create(
                    _updateProposalStateFormattableString,
                    (byte)targetStatus,
                    timeProvider.GetUtcNow(),
                    playerId?.Value!,
                    reason!,
                    proposalId.Value,
                    (byte)expectedStatus,
                    contextId.Value,
                    rating.AccStar!.Value.Value,
                    rating.DiffStar!.Value.Value,
                    rating.IsAccStarAutoCalculated,
                    rating.IsDiffStarAutoCalculated),
                token);

            if (updatedCount == 0)
                return new TransitionResponse.Conflict();

            var releaseBoosts = targetStatus == MapProposalStatus.Closed
                                && !await dbContext.MapProposals.OfType<SubmittedProposal>()
                                    .AnyAsync(x => x.ThreadId == proposal.ThreadId, token);

            if (releaseBoosts)
                await dbContext.MapRequestThreadBoosts.Where(x => x.ThreadId == proposal.ThreadId)
                    .ExecuteDeleteAsync(token);

            // Old instance couldn't change runtime type, we have to fetch it again.
            var updated = await GetProposal(contextId, proposalId).SingleAsync(token);
            await transaction.CommitAsync(token);

            // Invalidate the old subtype and its snapshots, leaving unrelated tracked entities alone.
            foreach (var entry in trackedConfiguration)
                entry.State = EntityState.Detached;

            if (!releaseBoosts)
                return new TransitionResponse.Success(updated);

            foreach (var entry in trackedBoosts)
                entry.State = EntityState.Detached;

            return new TransitionResponse.Success(updated);
        }
        catch (PostgresException exception)
            when (exception.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
        {
            return new TransitionResponse.Conflict();
        }
        catch (InvalidOperationException exception)
            when (exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.SerializationFailure
                                or PostgresErrorCodes.DeadlockDetected
                  })
        {
            // EF's non-retrying execution strategy wraps transient failures from bulk operations and queries.
            return new TransitionResponse.Conflict();
        }
    }

    public async Task<TransitionResponse> DeleteDraftAsync(
        ContextId contextId, MapProposalId proposalId, PlayerId playerId, CancellationToken token)
    {
        if (dbContext.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Save or discard pending changes before deleting a draft.");

        // Published proposals are immutable history. Only private drafts can be deleted.
        if (await dbContext.MapProposals.OfType<DraftProposal>()
                .Where(x => x.Id == proposalId && x.AuthorId == playerId && x.Thread.ContextId == contextId)
                .ExecuteDeleteAsync(token) > 0)
        {
            dbContext.ChangeTracker.Clear();
            return new TransitionResponse.Deleted();
        }

        return new TransitionResponse.NotFound();
    }

    private async Task<MapRequestResponses.DuplicateMatch[]> FindDuplicatesAsync(
        ContextId contextId, MapProposal proposal, CancellationToken token)
    {
        var ids = proposal.MapVersions.Select(x => x.SongDifficultyId).Distinct().ToArray();

        var maps = await dbContext.RankedMaps.Where(x => x.ContextId == contextId && x.ThreadId != proposal.ThreadId
                && x.MapVersions.Any(version => ((IEnumerable<SongDifficultyId>)ids)
                    .Contains(version.SongDifficultyId)))
            .Select(x => new MapRequestResponses.DuplicateMatch(x.ThreadId, x.Id, null))
            .ToArrayAsync(token);

        var proposals = await dbContext.MapProposals.OfType<SubmittedProposal>()
            .Where(x => x.Thread.ContextId == contextId
                        && x.ThreadId != proposal.ThreadId
                        && x.MapVersions.Any(version => ((IEnumerable<SongDifficultyId>)ids)
                            .Contains(version.SongDifficultyId)))
            .Select(x => new MapRequestResponses.DuplicateMatch(x.ThreadId, null, x.Id))
            .ToArrayAsync(token);

        return [.. maps, .. proposals];
    }

    private IQueryable<MapProposal> GetProposal(ContextId contextId, MapProposalId proposalId)
        => dbContext.MapProposals
            .Where(x => x.Id == proposalId && x.Thread.ContextId == contextId)
            .Include(x => x.MapVersions)
            .Include(x => x.Categories)
            .Include(x => x.Achievements)
            .AsSplitQuery();

    private async Task<Result<MapProposalRating>> ResolveRatingAsync(
        ContextId contextId, DraftProposal proposal, CancellationToken token)
    {
        var rating = proposal.Rating;
        if (rating is { AccStar: not null, DiffStar: not null })
            return rating with { IsAccStarAutoCalculated = false, IsDiffStarAutoCalculated = false };

        CustomCurve? accuracyCurve = null;
        if (rating.AccStar is null)
        {
            accuracyCurve = await dbContext.Points
                .Where(point => dbContext.Contexts
                    .Any(context => context.Id == contextId && context.GuildId == point.GuildId))
                .OrderBy(x => x.Id)
                .Select(x => x.CurveSettings.Accuracy)
                .FirstOrDefaultAsync(token);

            if (accuracyCurve is null)
                return Failure<MapProposalRating>("An accuracy curve is required to calculate accuracy stars.");
        }

        var sourceId = proposal.MapVersions.MinBy(x => x.Order)!.SongDifficultyId;
        var source = await (
            from difficulty in dbContext.SongDifficulties
            join song in dbContext.Songs on difficulty.SongId equals song.Id
            where difficulty.Id == sourceId
            select new { song.Hash, difficulty.Difficulty, GameMode = difficulty.GameMode.Name }
        ).SingleAsync(token);

        if (!(await beatLeaderApi.GetExMachinaStarRatingAsync(source.Hash, source.Difficulty, source.GameMode, token))
            .TryGetValue(out var exMachina, out var error))
            return Failure<MapProposalRating>(error);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (exMachina is null)
            return Failure<MapProposalRating>("No automatic rating is available for the proposal's map version.");

        var calculated = ScoringUtils.GetExMachinaRating(exMachina, proposal.Requirements);
        var passRating = calculated.LackMapCalculation.PassRating;
        var techRating = calculated.LackMapCalculation.TechRating;
        var validDifficulty = float.IsFinite(passRating)
                              && passRating >= 0
                              && float.IsFinite(techRating)
                              && techRating >= 0;

        var validAccuracy = rating.AccStar is not null || float.IsFinite(calculated.PredictedAcc);
        if (!validDifficulty || !validAccuracy)
            return Failure<MapProposalRating>("The automatic rating response contains invalid calculation values.");

        var accStar = rating.AccStar ?? calculated.ToAccStar(accuracyCurve!);
        var diffStar = rating.DiffStar ?? calculated.ToDiffStar();
        if (!float.IsFinite(accStar) || accStar < 0 || !float.IsFinite(diffStar) || diffStar < 0)
            return Failure<MapProposalRating>("The calculated ratings must be finite and non-negative.");

        return new MapProposalRating
        {
            AccStar = accStar,
            DiffStar = diffStar,
            IsAccStarAutoCalculated = rating.AccStar is null,
            IsDiffStarAutoCalculated = rating.DiffStar is null
        };
    }

    internal static TransitionResponse? GetTransitionError(
        MapProposal proposal, PlayerId? playerId, MapProposalStatus targetStatus)
        => proposal.AuthorId != playerId
            ? new TransitionResponse.Forbidden()
            : (proposal, targetStatus) switch
            {
                (DraftProposal, MapProposalStatus.Submitted) => null,
                (SubmittedProposal, MapProposalStatus.Closed) => null,
                _ => new TransitionResponse.InvalidState()
            };

    internal async Task<List<KeyValuePair<string, string[]>>> ValidateAsync(
        ContextId contextId, MapProposal proposal, CancellationToken token)
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
            .Select(x => x.Id)
            .ToArrayAsync(token);

        var achievementErrors = achievementIds.Except(achievements)
            .Select(id =>
                $"Achievement with ID '{id}' does not exist in the context or is not a ranked-map-list achievement.")
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

    internal static List<KeyValuePair<string, string[]>> GetConfigurationErrors(
        MapProposal proposal, Func<MapProposalVersion, SongDifficultyId>? difficultyIdSelector = null)
    {
        List<KeyValuePair<string, string[]>> errors = [];

        if (proposal.MapVersions.Count == 0)
        {
            errors.Add(new KeyValuePair<string, string[]>(nameof(MapProposal.MapVersions),
                ["A proposal must contain at least one map version."]));
        }
        else if (proposal.Rating.AccStar is null || proposal.Rating.DiffStar is null)
        {
            var firstOrder = proposal.MapVersions.Min(x => x.Order);
            if (proposal.MapVersions.Where(x => x.Order == firstOrder)
                .Select(difficultyIdSelector ?? (static version => version.SongDifficultyId)).Distinct().Skip(1).Any())
                errors.Add(new KeyValuePair<string, string[]>(nameof(MapProposal.MapVersions),
                    ["Automatic ratings require a single song difficulty at the lowest map-version order."]));
        }

        errors.AddRange(GetRequirementsAndRatingErrors(proposal.Requirements, proposal.Rating));
        return errors;
    }

    internal static List<KeyValuePair<string, string[]>> GetRequirementsAndRatingErrors(
        RankedMapRequirements requirements, MapProposalRating rating)
    {
        List<KeyValuePair<string, string[]>> errors = [];
        if (rating.AccStar is { } accStar && (!float.IsFinite(accStar) || accStar < 0))
            errors.Add(new KeyValuePair<string, string[]>("Rating.AccStar",
                ["Accuracy stars must be finite and non-negative."]));

        if (rating.DiffStar is { } diffStar && (!float.IsFinite(diffStar) || diffStar < 0))
            errors.Add(new KeyValuePair<string, string[]>("Rating.DiffStar",
                ["Difficulty stars must be finite and non-negative."]));

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