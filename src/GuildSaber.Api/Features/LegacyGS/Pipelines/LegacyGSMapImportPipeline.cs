using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedMaps.Pipelines;
using GuildSaber.Common.Services.LegacyGuildSaber;
using GuildSaber.Common.Services.LegacyGuildSaber.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using LegacyCategoryId = GuildSaber.Common.Services.LegacyGuildSaber.Models.Responses.RankingCategory.LegacyCategoryId;
using LegacyLevelId = GuildSaber.Common.Services.LegacyGuildSaber.Models.Responses.RankingLevel.LegacyLevelId;

namespace GuildSaber.Api.Features.LegacyGS.Pipelines;

public class LegacyGuildSaberMapImportPipeline(
    ServerDbContext dbContext,
    LegacyGuildSaberApi legacyGuildSaberApi,
    MapProposalDraftService draftService,
    MapProposalService proposalService,
    MapProposalReviewService reviewService,
    ApplyMapProposalPipeline applyPipeline,
    MemberPointStatsPipeline memberPointStatsPipeline,
    MemberAchievementStatsPipeline memberAchievementStatsPipeline,
    ILogger<LegacyGuildSaberMapImportPipeline> logger)
{
    private readonly record struct LegacyAchievementKey(
        LegacyLevelId LegacyLevelId,
        LegacyCategoryId LegacyCategoryId);

    /// <remarks>
    /// Importing legacy achievements with fractional orders is currently unsupported.
    /// (Solution: write a fallback to the default ordering SQL logic if such import is needed.)
    /// </remarks>
    public async Task ExecuteAsync(
        GuildId guildId,
        ContextId contextId,
        CancellationToken token)
    {
        logger.LogInformation("Starting legacy GuildSaber map import for guild {GuildId}", guildId);
        var request = new LegacyGuildSaberApi.PaginatedRequestOptions<RankedMapsSortBy>
        {
            Page = 1,
            PageSize = 10,
            MaxPage = int.MaxValue,
            SortBy = RankedMapsSortBy.EditedTime,
            Reverse = true
        };

        var categoryDict = await SyncCategoriesWithLegacyAsync(guildId, token);
        var achievementDict = await SyncAchievementsWithLegacyAsync(guildId, contextId, categoryDict, token);

        var rankedMapIdsToRemove = await dbContext.RankedMaps
            .Where(x => x.ContextId == contextId && x.Thread.IsLegacyImport)
            .Select(x => x.Id)
            .ToHashSetAsync(token);

        await foreach (var guildRankedMapsResult in legacyGuildSaberApi.GetGuildRankedMaps(guildId, request)
                           .WithCancellation(token))
        {
            if (!guildRankedMapsResult.TryGetValue(out var guildRankedMaps))
            {
                logger.LogWarning("Legacy map enumeration failed. Skipping deletion of unseen maps.");
                return;
            }

            foreach (var rankedMap in guildRankedMaps.Where(x => x.BeatSaverId is not null))
            foreach (var difficulty in rankedMap.Difficulties.Where(x => x.GameModeName is not null))
            {
                var existingRankedMaps = await dbContext.RankedMaps
                    .Include(x => x.Categories)
                    .Include(x => x.Achievements)
                    .Where(x => x.ContextId == contextId && x.Thread.IsLegacyImport)
                    .Where(MapDifficultyIsAlreadyRankedOnGuild(
                        guildId, difficulty.BeatSaverDifficultyValue, rankedMap.BeatSaverId!.Value,
                        difficulty.GameModeName!, dbContext))
                    .ToArrayAsync(token);

                var achievementKey = new LegacyAchievementKey(difficulty.LevelId, new LegacyCategoryId(0));
                if (!achievementDict.TryGetValue(achievementKey, out var achievement))
                    throw new InvalidOperationException("Cannot synchronize maps without their legacy achievements.");

                var requirements = new RankedMapRequests.RankedMapRequirements(
                    NeedConfirmation: difficulty.Requirements.HasFlag(ERequirements.NeedAdminConfirmation),
                    NeedFullCombo: difficulty.Requirements.HasFlag(ERequirements.FullCombo),
                    MaxPauseDurationSec: difficulty.Requirements.HasFlag(ERequirements.MaxPauses)
                        ? 2f
                        : null,
                    ProhibitedModifiers: ModifiersMapper.ToModifiers(difficulty.ProhibitedModifiers).Map(),
                    MandatoryModifiers: ModifiersMapper.ToModifiers(difficulty.MandatoryModifiers).Map(),
                    MinAccuracy: difficulty.MinScoreRequirement is not 0
                        ? (int)((float)difficulty.MinScoreRequirement / difficulty.MaxScore * 100f)
                        : null);

                var manualRating = new RankedMapRequests.ManualRating(
                    DifficultyStar: achievement.MinStar,
                    //TODO: Remove for production. Bypass BeatLeader ExMachina for the legacy import.
                    AccuracyStar: 0);

                int[] categoryIds = difficulty.GuildCategoryId.HasValue && difficulty.GuildCategoryId != 0
                    ? [categoryDict[difficulty.GuildCategoryId.Value].Id]
                    : [];

                // A difficulty may intentionally occur in multiple rankings. Do not merge or remove duplicates here.
                foreach (var current in existingRankedMaps)
                    rankedMapIdsToRemove.Remove(current.Id);

                var currentMap = existingRankedMaps.FirstOrDefault();
                if (currentMap is not null && !MapShouldBeUpdated(currentMap, requirements, manualRating, categoryIds))
                    continue;

                // Retry pending imports instead of opening another thread when acceptance failed.
                var threadId = currentMap?.ThreadId;
                var pendingProposalId = await GetPendingProposalIdAsync(
                    contextId, threadId, difficulty.BeatSaverDifficultyValue, rankedMap.BeatSaverId.Value,
                    difficulty.GameModeName!, token);

                var proposalRequest = new MapProposalRequests.CreateDraft(
                    MapVersions: currentMap is null
                        ?
                        [
                            new MapProposalRequests.MapVersionInput(
                                new MapProposalRequests.SongReference.ByKey(rankedMap.BeatSaverId.Value),
                                difficulty.GameModeName!, difficulty.BeatSaverDifficultyValue, "Standard", 0)
                        ]
                        : await GetMapVersionsAsync(currentMap.Id, token),
                    Requirements: requirements,
                    Rating: manualRating with
                    {
                        AccuracyStar = currentMap?.Rating.AccStar.Value ?? manualRating.AccuracyStar
                    },
                    CategoryIds: categoryIds.Select(id => new CategoryId(id)).ToArray(),
                    AchievementIds: []);

                await ImportProposalAsync(contextId, threadId, pendingProposalId, proposalRequest, token);
            }
        }

        if (rankedMapIdsToRemove.Count == 0)
        {
            logger.LogInformation("No ranked maps to remove for guild {GuildId}", guildId);
            logger.LogInformation("Completed legacy GuildSaber map import for guild {GuildId}", guildId);
            return;
        }

        logger.LogInformation(
            "Removing {Count} ranked maps that are no longer present on legacy GuildSaber for guild {GuildId}",
            rankedMapIdsToRemove.Count, guildId);
        var (playerIds, contextsWithPoints) = (
            await dbContext.RankedMaps
                .Where(x => rankedMapIdsToRemove.Contains(x.Id))
                .SelectMany(x => x.RankedScores.Select(y => y.PlayerId))
                .Distinct()
                .ToArrayAsync(token),
            await dbContext.Contexts
                .Include(x => x.Points)
                .Where(x => x.GuildId == guildId)
                .ToArrayAsync(token)
        );

        var affectedRows = await dbContext.RankedMaps.Where(x => rankedMapIdsToRemove.Contains(x.Id))
            .ExecuteDeleteAsync(token);

        if (affectedRows != rankedMapIdsToRemove.Count)
            logger.LogWarning(
                "Expected to delete {ExpectedCount} ranked maps but actually deleted {ActualCount} ranked maps for guild {GuildId}",
                rankedMapIdsToRemove.Count, affectedRows, guildId);
        else
            logger.LogInformation("Deleted {DeletedCount} ranked maps for guild {GuildId}", affectedRows, guildId);

        logger.LogInformation("Recalculating member stats for affected players in guild {GuildId}", guildId);
        foreach (var playerId in playerIds)
        foreach (var context in contextsWithPoints)
        {
            await memberPointStatsPipeline.ExecuteAsync(playerId, context);
            foreach (var point in context.Points)
                await memberAchievementStatsPipeline.ExecuteAsync(playerId, context.GuildId, context.Id, point.Id);
        }

        logger.LogInformation("Completed recalculating member stats for affected players in guild {GuildId}", guildId);
        logger.LogInformation("Completed legacy GuildSaber map import for guild {GuildId}", guildId);
    }

    private static bool MapShouldBeUpdated(
        RankedMap currentMap, RankedMapRequests.RankedMapRequirements requirements,
        RankedMapRequests.ManualRating manualRating, int[] categoryIds)
    {
        if (!currentMap.Categories.Select(x => (int)x.Id).ToHashSet().SetEquals(categoryIds))
            return true;

        // Legacy star achievements are dynamic. Any direct association is stale.
        if (currentMap.Achievements.Count != 0)
            return true;

        // Requirements being a record, we can just compare them directly for equality.
        if (currentMap.Requirements.Map() != requirements)
            return true;

        Trace.Assert(manualRating.DifficultyStar.HasValue, "All maps on legacy GS has a difficulty.");
        return Math.Abs(currentMap.Rating.DiffStar.Value - manualRating.DifficultyStar.Value) > 0.01f;
    }

    private Task<MapProposalId?> GetPendingProposalIdAsync(
        ContextId contextId, MapRequestThreadId? threadId, EDifficulty difficulty, BeatSaverKey beatSaverKey,
        string gameMode, CancellationToken token)
        => dbContext.MapProposals
            .Where(x => x.Thread.ContextId == contextId && x.Thread.IsLegacyImport && x.AuthorId == null
                        && (x is DraftProposal || x is SubmittedProposal))
            .Where(x => threadId == null ? x.Thread.RankedMap == null : x.ThreadId == threadId)
            .Where(x => x.MapVersions.Any(version =>
                version.SongDifficulty.GameMode.Name.Contains(gameMode)
                && version.SongDifficulty.Difficulty == difficulty
                && version.SongDifficulty.Song.BeatSaverKey == beatSaverKey))
            .OrderBy(x => x.Id)
            .Select(x => (MapProposalId?)x.Id)
            .FirstOrDefaultAsync(token);

    private Task<MapProposalRequests.MapVersionInput[]> GetMapVersionsAsync(
        RankedMapId rankedMapId, CancellationToken token)
        => dbContext.MapVersions
            .Where(x => x.RankedMapId == rankedMapId)
            .Select(x => new MapProposalRequests.MapVersionInput(
                new MapProposalRequests.SongReference.ByHash(x.Song.Hash),
                x.SongDifficulty.GameMode.Name,
                x.SongDifficulty.Difficulty,
                dbContext.PlayModes.Where(mode => mode.Id == x.PlayModeId).Select(mode => mode.Name).First(),
                x.Order))
            .ToArrayAsync(token);

    private async Task ImportProposalAsync(
        ContextId contextId, MapRequestThreadId? threadId, MapProposalId? proposalId,
        MapProposalRequests.CreateDraft request, CancellationToken token)
    {
        if (proposalId is null)
        {
            MapProposalDraftService.CreateResponse creation = new MapProposalDraftService.CreateResponse.Conflict();

            for (var attempt = 0; attempt < 3; attempt++)
            {
                creation = await draftService.CreateImportedAsync(contextId, threadId, request, token);
                if (creation is not MapProposalDraftService.CreateResponse.RateLimited limited)
                    break;

                await Task.Delay(limited.RetryAfter, token);
            }

            if (creation is not MapProposalDraftService.CreateResponse.Success(var draft))
            {
                logger.LogWarning("Legacy proposal creation failed in context {ContextId}: {Result}", contextId,
                    creation);
                return;
            }

            proposalId = draft.Id;
        }

        if (await dbContext.MapProposals.OfType<DraftProposal>().AnyAsync(x => x.Id == proposalId, token))
        {
            var submission = await proposalService.SubmitImportedAsync(contextId, proposalId.Value, token);
            if (submission is not MapProposalService.TransitionResponse.Success)
            {
                logger.LogWarning("Legacy proposal submission failed: {Result}", submission);
                await dbContext.MapProposals
                    .OfType<DraftProposal>()
                    .Where(x => x.Id == proposalId && x.AuthorId == null)
                    .ExecuteDeleteAsync(token);

                dbContext.ChangeTracker.Clear();
                return;
            }
        }

        var review = await reviewService.AcceptImportedAsync(contextId, proposalId.Value, token);
        if (review is not MapProposalReviewService.ReviewResponse.Accepted accepted)
        {
            logger.LogWarning("Legacy proposal {ProposalId} remains submitted for review: {Result}", proposalId,
                review);
            return;
        }

        await applyPipeline.ExecuteAsync(accepted.RankedMapId, accepted.AddedDifficultyIds, token);
    }

    public async Task<Dictionary<LegacyCategoryId, Category>> SyncCategoriesWithLegacyAsync(
        GuildId guildId, CancellationToken token)
    {
        var categories = await dbContext.Categories
            .AsTracking()
            .Where(x => x.GuildId == guildId)
            .ToListAsync(token);

        if (!(await legacyGuildSaberApi.GetRankingCategoriesAsync(guildId)).TryGetValue(out var legacyCategories))
            return [];

        foreach (var oldCategory in legacyCategories)
        {
            var info = new CategoryInfo(Name_2_50.CreateUnsafe(oldCategory.Name).Value,
                Description.CreateUnsafe(oldCategory.Description).Value);

            var category = categories.FirstOrDefault(x => x.Info.Name == info.Name);
            if (category is null)
            {
                category = new Category { GuildId = guildId };
                dbContext.Categories.Add(category);
                categories.Add(category);
            }

            category.Info = info;
        }

        await dbContext.SaveChangesAsync(token);
        dbContext.ChangeTracker.Clear();

        return legacyCategories
            .Join(categories, o => o.Name, n => n.Info.Name, (o, n) => (Old: o, New: n))
            .ToDictionary(x => x.Old.Id, x => x.New);
    }

    private async Task<Dictionary<LegacyAchievementKey, DiffStarAchievement>> SyncAchievementsWithLegacyAsync(
        GuildId guildId, ContextId contextId, Dictionary<LegacyCategoryId, Category> categories,
        CancellationToken token)
    {
        var achievements = await dbContext.Achievements
            .AsTracking()
            .Where(x => x.GuildId == guildId && x.ContextId == contextId)
            .OfType<DiffStarAchievement>()
            .ToListAsync(token);

        if (!(await legacyGuildSaberApi.GetRankingLevelsAsync(guildId)).TryGetValue(out var legacyLevels))
            return [];

        var result = new Dictionary<LegacyAchievementKey, DiffStarAchievement>();
        foreach (var legacyLevel in legacyLevels)
        {
            var minStar = new RankedMapRating.DifficultyStar(legacyLevel.LevelNumber);
            var maxStar = GetLegacyExclusiveMaxStar(legacyLevel.LevelNumber);
            var progressionOrder = GetLegacyProgressionOrder(legacyLevel.LevelNumber);
            var achievementName = $"Lvl {legacyLevel.LevelNumber:G}";
            var achievement = achievements.FirstOrDefault(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.CategoryId == null &&
                x.Info.Name == achievementName);
            if (achievement is null)
            {
                achievement = new DiffStarAchievement(
                    id: default,
                    guildId: guildId,
                    contextId: contextId,
                    categoryId: null,
                    info: new AchievementInfo
                    {
                        Name = Name_2_50.CreateUnsafe(achievementName).Value,
                        Color = Color.FromArgb(legacyLevel.Color)
                    },
                    discordBindings: new AchievementDiscordBindings(DiscordRoleId.TryCreate(legacyLevel.DiscordRoleId)
                        .Match(roleId => (DiscordRoleId?)roleId, _ => null)),
                    progressionOrder: progressionOrder,
                    isLocking: progressionOrder is not null,
                    unlockXp: Xp.TryCreate(0).GetValueOrDefault(),
                    requiredPassCount: 1,
                    minStar: minStar,
                    maxStar: maxStar
                );

                dbContext.Achievements.Add(achievement);
            }
            else
            {
                achievement.Info = achievement.Info with { Color = Color.FromArgb(legacyLevel.Color) };
                achievement.DiscordBindings = new AchievementDiscordBindings(DiscordRoleId
                    .TryCreate(legacyLevel.DiscordRoleId)
                    .Match(roleId => (DiscordRoleId?)roleId, _ => null));
            }

            achievement.MinStar = minStar;
            achievement.MaxStar = maxStar;
            achievement.ProgressionOrder = progressionOrder;
            achievement.IsLocking = progressionOrder is not null;

            result[new LegacyAchievementKey(legacyLevel.Id, new LegacyCategoryId(0))] = achievement;

            foreach (var (legacyCategoryId, category) in categories)
            {
                var categoryAchievement = achievements.FirstOrDefault(x =>
                    x.GuildId == guildId &&
                    x.ContextId == contextId &&
                    x.CategoryId == category.Id &&
                    x.Info.Name == achievementName);
                if (categoryAchievement is null)
                {
                    categoryAchievement = new DiffStarAchievement(
                        id: default,
                        guildId: guildId,
                        contextId: contextId,
                        categoryId: category.Id,
                        info: new AchievementInfo
                        {
                            Name = Name_2_50.CreateUnsafe(achievementName).Value,
                            Color = Color.FromArgb(legacyLevel.Color)
                        },
                        discordBindings: new AchievementDiscordBindings(DiscordRoleId
                            .TryCreate(legacyLevel.DiscordRoleId)
                            .GetValueOrDefault()),
                        progressionOrder: progressionOrder,
                        isLocking: progressionOrder is not null,
                        unlockXp: Xp.CreateUnsafe(0).Value,
                        minStar: minStar,
                        requiredPassCount: 1,
                        maxStar: maxStar
                    );

                    dbContext.Achievements.Add(categoryAchievement);
                }
                else
                {
                    categoryAchievement.Info = categoryAchievement.Info with
                    {
                        Color = Color.FromArgb(legacyLevel.Color)
                    };
                    categoryAchievement.DiscordBindings = new AchievementDiscordBindings(DiscordRoleId
                        .TryCreate(legacyLevel.DiscordRoleId)
                        .GetValueOrDefault());
                }

                categoryAchievement.MinStar = minStar;
                categoryAchievement.MaxStar = maxStar;
                categoryAchievement.ProgressionOrder = progressionOrder;
                categoryAchievement.IsLocking = progressionOrder is not null;

                result[new LegacyAchievementKey(legacyLevel.Id, legacyCategoryId)] = categoryAchievement;
            }
        }

        await dbContext.SaveChangesAsync(token);
        dbContext.ChangeTracker.Clear();

        return result;
    }

    internal static RankedMapRating.DifficultyStar GetLegacyExclusiveMaxStar(float minStar)
        => new(MathF.Floor(minStar) + 1);

    internal static uint? GetLegacyProgressionOrder(float legacyLevelNumber)
        => legacyLevelNumber.Equals(100f) ? null : (uint)Math.Round(legacyLevelNumber);

    private static Expression<Func<RankedMap, bool>> MapDifficultyIsAlreadyRankedOnGuild(
        GuildId guildId, EDifficulty difficulty, BeatSaverKey beatSaverKey, string gameMode,
        ServerDbContext dbContext)
        => rankedMap => rankedMap.GuildId == guildId && rankedMap.MapVersions.Any(y =>
            y.SongDifficulty.GameMode.Name.Contains(gameMode)
            && y.SongDifficulty.Difficulty == difficulty
            && dbContext.Songs.Any(z => z.Id == y.SongId && z.BeatSaverKey == beatSaverKey));
}