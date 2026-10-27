using System.Data;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.Songs;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.BeatSaver.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Songs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using AchievementId = GuildSaber.Database.Models.Server.Guilds.Achievements.Achievement.AchievementId;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;

namespace GuildSaber.Api.Features.MapRequests.Services;

public sealed class MapProposalDraftService(
    ServerDbContext dbContext,
    BeatSaverApi beatSaverApi,
    SongService songService,
    TimeProvider timeProvider,
    IOptions<MapRequestSettings> settings)
{
    public closed record CreateResponse
    {
        public sealed record Success(DraftProposal Proposal) : CreateResponse;
        public sealed record ContextNotFound : CreateResponse;
        public sealed record ThreadNotFound : CreateResponse;
        public sealed record PlayerNotFound : CreateResponse;
        public sealed record ValidationFailure(IEnumerable<KeyValuePair<string, string[]>> Errors) : CreateResponse;
        public sealed record NotOnBeatSaver(SongReference Song) : CreateResponse;
        public sealed record BeatSaverFailure(string Message) : CreateResponse;
        public sealed record RateLimited(TimeSpan RetryAfter) : CreateResponse;
        public sealed record ResolutionFailure(int VersionIndex, string Message) : CreateResponse;
        public sealed record Conflict : CreateResponse;
        public sealed record LimitReached(int Limit) : CreateResponse;
    }

    /// <summary>Creates a complete draft, optionally in an existing thread in the same context.</summary>
    /// <remarks>Resolves song identities now, but leaves missing ratings for submission. Does not publish to the queue.</remarks>
    public Task<CreateResponse> CreateAsync(
        ContextId contextId, MapRequestThreadId? threadId, PlayerId playerId, CreateDraft request,
        CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (service: this, contextId, threadId, playerId, request),
            static (state, cancellationToken) => state.service.CreateCoreAsync(
                state.contextId, state.threadId, state.playerId, state.request, cancellationToken), token);

    /// <summary>Trusted legacy import only. No player is impersonated for automated history.</summary>
    internal Task<CreateResponse> CreateImportedAsync(
        ContextId contextId, MapRequestThreadId? threadId, CreateDraft request, CancellationToken token)
        => dbContext.Database.CreateExecutionStrategy().ExecuteAsync((service: this, contextId, threadId, request),
            static (state, cancellationToken) => state.service.CreateCoreAsync(state.contextId, state.threadId,
                null, state.request, cancellationToken), token);

    private async Task<CreateResponse> CreateCoreAsync(
        ContextId contextId, MapRequestThreadId? threadId, PlayerId? playerId, CreateDraft request,
        CancellationToken token)
    {
        if (dbContext.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Save or discard pending changes before creating a draft.");

        var committed = false;
        try
        {
            var errors = GetInputErrors(request);
            if (errors.Count > 0)
                return new CreateResponse.ValidationFailure(errors);

            if (!request.Requirements.Map().TryGetValue(out var requirements, out var requirementErrors))
                return new CreateResponse.ValidationFailure(requirementErrors);

            var rating = new MapProposalRating
            {
                AccStar = request.Rating.AccuracyStar is { } accStar ? new RankedMapRating.AccuracyStar(accStar) : null,
                DiffStar = request.Rating.DifficultyStar is { } diffStar
                    ? new RankedMapRating.DifficultyStar(diffStar)
                    : null
            };

            errors = MapProposalService.GetRequirementsAndRatingErrors(requirements, rating);
            if (errors.Count > 0)
                return new CreateResponse.ValidationFailure(errors);

            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, token);

            var context = await dbContext.Contexts.Where(x => x.Id == contextId)
                .Select(x => new { x.GuildId })
                .SingleOrDefaultAsync(token);

            if (context is null)
                return new CreateResponse.ContextNotFound();

            if (playerId is not null && !await dbContext.Players.AnyAsync(x => x.Id == playerId, token))
                return new CreateResponse.PlayerNotFound();

            if (threadId is { } existing && !await dbContext.MapRequestThreads
                    .AnyAsync(x => x.Id == existing
                                   && x.ContextId == contextId
                                   && (playerId == null
                                       || x.AuthorId == playerId
                                       || x.Proposals.Any(proposal =>
                                           proposal is SubmittedProposal
                                           || proposal is AcceptedProposal
                                           || proposal is ClosedProposal
                                           && ((ClosedProposal)proposal).SubmittedAt != null)), token))
                return new CreateResponse.ThreadNotFound();


            if (playerId is null && threadId is { } importedThread && !await dbContext.MapRequestThreads
                    .AnyAsync(x => x.Id == importedThread && x.IsLegacyImport, token))
                return new CreateResponse.ThreadNotFound();

            if (playerId is not null && await dbContext.MapProposals.OfType<DraftProposal>()
                    .CountAsync(x => x.AuthorId == playerId && x.Thread.ContextId == contextId, token)
                >= settings.Value.MaxDraftsPerPlayer)
                return new CreateResponse.LimitReached(settings.Value.MaxDraftsPerPlayer);

            var categoryIds = request.CategoryIds;
            var categories = await dbContext.Categories
                .Where(x => ((IEnumerable<CategoryId>)categoryIds).Contains(x.Id) && x.GuildId == context.GuildId)
                .Select(x => new MapProposalCategory { CategoryId = x.Id, Name = x.Info.Name }).ToArrayAsync(token);
            var achievementIds = request.AchievementIds.Select(x => new AchievementId(x)).ToArray();
            var achievements = await dbContext.Achievements.OfType<RankedMapListAchievement>()
                .Where(x => ((IEnumerable<AchievementId>)achievementIds).Contains(x.Id)
                            && x.GuildId == context.GuildId && x.ContextId == contextId)
                .Select(x => new MapProposalAchievement { AchievementId = x.Id, Name = x.Info.Name })
                .ToArrayAsync(token);

            if (categories.Length != categoryIds.Length)
                errors.Add(new KeyValuePair<string, string[]>(nameof(request.CategoryIds),
                    ["Each category must exist in the thread's guild."]));

            if (achievements.Length != achievementIds.Length)
                errors.Add(new KeyValuePair<string, string[]>(nameof(request.AchievementIds),
                    ["Each achievement must be a ranked-map-list achievement in the thread's context."]));

            if (errors.Count > 0)
                return new CreateResponse.ValidationFailure(errors);

            List<MapProposalVersion> versions = [];
            for (var index = 0; index < request.MapVersions.Length; index++)
            {
                var input = request.MapVersions[index];
                var gameMode = await dbContext.GameModes
                    .SingleOrDefaultAsync(x => x.Name == input.Characteristic, token);

                var playMode = await dbContext.PlayModes.SingleOrDefaultAsync(x => x.Name == input.PlayMode, token);
                if (gameMode is null || playMode is null)
                    return new CreateResponse.ValidationFailure([
                        new KeyValuePair<string, string[]>($"MapVersions[{index}]",
                            ["The characteristic and play mode must both be supported."])
                    ]);

                if (!(await ResolveBeatMapAsync(input.Song, token)).TryGetValue(out var beatMap, out var beatMapError))
                    return beatMapError;

                var selector = input.Song switch
                {
                    SongReference.ByKey => BeatSaverApi.SelectLatestBeatMapVersion,
                    SongReference.ByHash(var hash) => BeatSaverApi.SelectBeatMapVersionByHash(hash)
                };

                if (!(await songService.GetOrCreateDifficultyAsync(
                        beatMap, selector, gameMode, input.Difficulty, token))
                    .TryGetValue(out var resolved, out var resolutionError))
                    return new CreateResponse.ResolutionFailure(index, resolutionError);

                if (versions.Any(x => ReferenceEquals(x.SongDifficulty, resolved.difficulty)
                                      && x.PlayModeId == playMode.Id))
                    return new CreateResponse.ValidationFailure([
                        new KeyValuePair<string, string[]>($"MapVersions[{index}]",
                            ["The same song difficulty and play mode cannot appear twice."])
                    ]);

                versions.Add(new MapProposalVersion
                {
                    SongDifficulty = resolved.difficulty, PlayModeId = playMode.Id,
                    PlayModeName = playMode.Name, Order = input.Order
                });
            }

            var now = timeProvider.GetUtcNow();
            var proposal = new DraftProposal
            {
                ThreadId = threadId.GetValueOrDefault(),
                Thread = threadId is null
                    ? new MapRequestThread
                    {
                        ContextId = contextId, AuthorId = playerId, CreatedAt = now, IsLegacyImport = playerId is null
                    }
                    : null!,
                AuthorId = playerId, CreatedAt = now, Requirements = requirements, Rating = rating,
                Comment = request.Comment,
                MapVersions = versions, Categories = categories, Achievements = achievements, Votes = []
            };
            dbContext.MapProposals.Add(proposal);

            // New difficulties have distinct temporary IDs in EF, while their CLR key properties are still zero.
            errors = MapProposalService.GetConfigurationErrors(proposal, version => dbContext.Entry(version)
                .Property(x => x.SongDifficultyId).CurrentValue);

            if (errors.Count > 0)
                return new CreateResponse.ValidationFailure(errors);

            await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);
            await transaction.CommitAsync(token);

            dbContext.ChangeTracker.AcceptAllChanges();
            committed = true;

            return new CreateResponse.Success(proposal);
        }
        catch (Exception exception)
            when ((exception as PostgresException ??
                   exception.InnerException as PostgresException)
                  ?.SqlState is PostgresErrorCodes.SerializationFailure
                                or PostgresErrorCodes.DeadlockDetected
                                or PostgresErrorCodes.UniqueViolation
                                or PostgresErrorCodes.ForeignKeyViolation)
        {
            return new CreateResponse.Conflict();
        }
        finally
        {
            if (!committed) DiscardDraftChanges();
        }
    }

    private Task<Result<BeatMap, CreateResponse>> ResolveBeatMapAsync(SongReference song, CancellationToken token)
        => (song switch
            {
                SongReference.ByKey(var key) => beatSaverApi.GetBeatMapAsync(key, token),
                SongReference.ByHash(var hash) => beatSaverApi.GetBeatMapAsync(hash, token)
            })
            .MapError(CreateResponse (error) => error switch
            {
                BeatSaverApi.Error.RateLimitExceeded(var retryAfter) => new CreateResponse.RateLimited(retryAfter),
                BeatSaverApi.Error.Other(var message) => new CreateResponse.BeatSaverFailure(message),
                _ => throw new NotSupportedException($"Unhandled BeatSaver API error: {error}")
            })
            .Bind(beatMap => beatMap is null
                ? Failure<BeatMap, CreateResponse>(new CreateResponse.NotOnBeatSaver(song))
                : Success<BeatMap, CreateResponse>(beatMap));

    private static List<KeyValuePair<string, string[]>> GetInputErrors(CreateDraft request)
    {
        List<KeyValuePair<string, string[]>> errors = [];
        if (request.MapVersions.Length == 0)
            errors.Add(new KeyValuePair<string, string[]>(nameof(request.MapVersions),
                ["A draft must contain at least one map version."]));

        for (var index = 0; index < request.MapVersions.Length; index++)
        {
            var version = request.MapVersions[index];
            if (string.IsNullOrWhiteSpace(version.Characteristic)
                || string.IsNullOrWhiteSpace(version.PlayMode)
                || !Enum.IsDefined(version.Difficulty))
                errors.Add(new KeyValuePair<string, string[]>($"MapVersions[{index}]",
                    ["A characteristic, difficulty and play mode are required."]));
        }

        if (request.CategoryIds.Distinct().Count() != request.CategoryIds.Length)
            errors.Add(new KeyValuePair<string, string[]>(nameof(request.CategoryIds),
                ["Category IDs must be distinct."]));

        if (request.AchievementIds.Distinct().Count() != request.AchievementIds.Length)
            errors.Add(new KeyValuePair<string, string[]>(nameof(request.AchievementIds),
                ["Achievement IDs must be distinct."]));

        return errors;
    }

    private void DiscardDraftChanges()
    {
        // Creation starts with no pending changes, so only this operation's additions/backfills need undoing.
        var entries = dbContext.ChangeTracker.Entries().ToArray();
        var added = entries.Where(x => x.State == EntityState.Added).Select(x => x.Entity)
            .ToHashSet(ReferenceEqualityComparer.Instance);

        foreach (var song in entries.Select(x => x.Entity).OfType<Song>())
            if (song.SongDifficulties is { } difficulties)
                foreach (var difficulty in difficulties.Where(added.Contains).ToArray())
                    difficulties.Remove(difficulty);

        foreach (var thread in entries.Select(x => x.Entity).OfType<MapRequestThread>())
            if (thread.Proposals is { } proposals)
                foreach (var proposal in proposals.Where(added.Contains).ToArray())
                    proposals.Remove(proposal);

        foreach (var entry in entries)
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Modified:
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                    break;
            }
    }
}