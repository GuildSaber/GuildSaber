using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedMaps.MapVersions.Http;
using GuildSaber.Common.Services.LegacyGuildSaber;
using GuildSaber.Common.Services.LegacyGuildSaber.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using LegacyCategoryId = GuildSaber.Common.Services.LegacyGuildSaber.Models.Responses.RankingCategory.LegacyCategoryId;
using LegacyLevelId = GuildSaber.Common.Services.LegacyGuildSaber.Models.Responses.RankingLevel.LegacyLevelId;
using static GuildSaber.Api.Features.RankedMaps.RankedMapService;

namespace GuildSaber.Api.Features.LegacyGS.Pipelines;

public class LegacyGuildSaberMapImportPipeline(
    ServerDbContext dbContext,
    LegacyGuildSaberApi legacyGuildSaberApi,
    RankedMapService rankedMapService,
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

        var rankedMapIdsToRemove = await dbContext.RankedMaps.Where(x => x.GuildId == guildId)
            .Select(x => x.Id)
            .ToHashSetAsync(token);

        await foreach (var guildRankedMapsResult in legacyGuildSaberApi.GetGuildRankedMaps(guildId, request)
                           .WithCancellation(token))
        {
            if (!guildRankedMapsResult.TryGetValue(out var guildRankedMaps))
                break;

            foreach (var rankedMap in guildRankedMaps.Where(x => x.BeatSaverId is not null))
            foreach (var difficulty in rankedMap.Difficulties.Where(x => x.GameModeName is not null))
            {
                var existingRankedMaps = await dbContext.RankedMaps
                    .Include(x => x.Categories)
                    .Include(x => x.Achievements)
                    .Where(MapDifficultyIsAlreadyRankedOnGuild(
                        guildId, difficulty.BeatSaverDifficultyValue, rankedMap.BeatSaverId!.Value,
                        difficulty.GameModeName!, dbContext))
                    .ToArrayAsync(token);

                var achievementKey = new LegacyAchievementKey(difficulty.LevelId, new LegacyCategoryId(0));
                var achievement = achievementDict[achievementKey];

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
                    AccuracyStar: null);
                int[] categoryIds = difficulty.GuildCategoryId.HasValue && difficulty.GuildCategoryId != 0
                    ? [categoryDict[difficulty.GuildCategoryId.Value].Id]
                    : [];

                if (existingRankedMaps.Length > 0)
                {
                    var currentMap = existingRankedMaps[0];
                    rankedMapIdsToRemove.Remove(currentMap.Id);

                    if (MapShouldBeUpdated(currentMap, requirements, manualRating, categoryIds))
                        _ = await UpdateMapAsync(currentMap.Id, contextId,
                            new RankedMapRequests.UpdateRankedMap(
                                Requirements: requirements,
                                ManualRating: manualRating,
                                CategoryIds: categoryIds,
                                AchievementIds: []), token);

                    continue;
                }

                if (existingRankedMaps.Length > 1)
                {
                    logger.LogWarning(
                        "Found {Count} existing ranked maps for BeatSaver map {BeatSaverKey} difficulty {Difficulty} characteristic {Characteristic} in guild {GuildId}." +
                        "Removing duplicates and keeping the most recently edited one.",
                        existingRankedMaps.Length, rankedMap.BeatSaverId, difficulty.BeatSaverDifficultyName,
                        difficulty.GameModeName, guildId);

                    // Yes we don't care about member stats recalculation, since it's not worth it. 
                    dbContext.RankedMaps.RemoveRange(existingRankedMaps.Skip(1));
                }

                _ = await RankMapWithRetryAsync(contextId, new RankedMapRequests.CreateRankedMap(
                    BaseMapVersion: new MapVersionRequests.AddMapVersion(
                        BeatSaverKey: rankedMap.BeatSaverId.Value,
                        Characteristic: difficulty.GameModeName!,
                        Difficulty: difficulty.BeatSaverDifficultyValue,
                        PlayMode: "Standard",
                        Order: 0),
                    ManualRating: manualRating,
                    Requirements: requirements,
                    CategoryIds: categoryIds,
                    AchievementIds: []
                ), retryCount: 3, token);
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

    private bool MapShouldBeUpdated(
        RankedMap currentMap, RankedMapRequests.RankedMapRequirements requirements,
        RankedMapRequests.ManualRating manualRating, int[] categoryIds)
    {
        if (!currentMap.Categories.All(x => categoryIds.Contains(x.Id)))
            return true;

        // Legacy star achievements are dynamic; any direct association is stale.
        if (currentMap.Achievements.Count != 0)
            return true;

        // Requirements being a record, we can just compare them directly for equality.
        if (currentMap.Requirements.Map() != requirements)
            return true;

        Trace.Assert(manualRating.DifficultyStar.HasValue, "All maps on legacy GS has a difficulty.");
        return Math.Abs(currentMap.Rating.DiffStar.Value - manualRating.DifficultyStar.Value) > 0.01f;
    }

    public async Task<bool> UpdateMapAsync(
        RankedMapId rankedMapId, ContextId contextId, RankedMapRequests.UpdateRankedMap request,
        CancellationToken token)
    {
        var result = await rankedMapService.UpdateRankedMapAsync(rankedMapId, contextId, request);
        switch (result)
        {
            case UpdateResponse.Success:
                logger.LogInformation("Updated ranked map {RankedMapId} for context {ContextId}",
                    rankedMapId, contextId);
                return true;
            case UpdateResponse.NotFound:
                logger.LogWarning("Ranked map {RankedMapId} not found for update", rankedMapId);
                return false;
            case UpdateResponse.ValidationFailure validationFailure:
                logger.LogWarning(
                    "Validation failed when updating ranked map {RankedMapId} for contextId {ContextId}: {Errors}",
                    rankedMapId, contextId, string.Join(", ", validationFailure.Errors)
                );
                return false;
            case UpdateResponse.UnexpectedFailure failure:
                logger.LogError(
                    "Unexpected failure when updating ranked map {RankedMapId} for contextId {ContextId}: {ErrorMessage}",
                    rankedMapId, contextId, failure.Message);
                return false;
            default: throw new UnreachableException();
        }
    }

    public async Task<bool> RankMapWithRetryAsync(
        ContextId contextId, RankedMapRequests.CreateRankedMap request, int retryCount, CancellationToken token)
    {
        do
        {
            var result = await rankedMapService.CreateRankedMapAsync(contextId, request);
            switch (result)
            {
                case CreateResponse.Success success:
                    logger.LogInformation(
                        "Imported BeatSaver map {BeatSaverKey} difficulty {Difficulty} characteristic {Characteristic} as ranked map {RankedMapId} for guild {GuildId}",
                        request.BaseMapVersion.BeatSaverKey, request.BaseMapVersion.Difficulty,
                        request.BaseMapVersion.Characteristic, success.RankedMap.Id, success.RankedMap.GuildId);
                    return true;
                case CreateResponse.RateLimited limited:
                    logger.LogWarning("Rate limited when creating ranked map, retrying in {RetryAfter}ms",
                        limited.RetryAfter);
                    await Task.Delay(limited.RetryAfter, token);
                    continue;
                case CreateResponse.NotOnBeatSaver notOnBeatSaver:
                    logger.LogWarning("Map not on BeatSaver: {BeatSaverKey}", notOnBeatSaver.BeatSaverKey);
                    return false;
                case CreateResponse.ValidationFailure validationFailure:
                    logger.LogWarning(
                        "Validation failed when importing BeatSaver map {BeatSaverKey} difficulty {Difficulty} characteristic {Characteristic} for contextId {ContextId}: {Errors}",
                        request.BaseMapVersion.BeatSaverKey, request.BaseMapVersion.Difficulty,
                        request.BaseMapVersion.Characteristic, contextId, string.Join(", ", validationFailure.Errors)
                    );
                    return false;
                case CreateResponse.UnexpectedFailure failure:
                    logger.LogError(
                        "Unexpected failure when creating ranked map with BeatSaverKey {BeatSaverKey} difficulty {Difficulty} for contextId {ContextId}: {ErrorMessage}",
                        request.BaseMapVersion.BeatSaverKey, request.BaseMapVersion.Difficulty, contextId,
                        failure.Message);
                    break;
                case CreateResponse.TooManyRankedMaps tooMany:
                    logger.LogWarning(
                        "Too many ranked maps ({CurrentCount}/{MaxCount}) in contextId {ContextId}",
                        tooMany.CurrentCount, tooMany.MaxCount, contextId);
                    return false;
                default: throw new UnreachableException();
            }
        } while (retryCount++ < 3);

        logger.LogError(
            "Exceeded maximum retries when creating ranked map with BeatSaverKey {BeatSaverKey} difficulty {Difficulty} for contextId {ContextId}",
            request.BaseMapVersion.BeatSaverKey, request.BaseMapVersion.Difficulty, contextId);
        return false;
    }

    public async Task<Dictionary<LegacyCategoryId, Category>> SyncCategoriesWithLegacyAsync(
        GuildId guildId, CancellationToken token)
    {
        var categories = await dbContext.Categories.AsTracking().Where(x => x.GuildId == guildId).ToListAsync(token);
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
                achievement = new DiffStarAchievement
                (
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
                var categoryAchievementName = $"Lvl {legacyLevel.LevelNumber:G}";
                var categoryAchievement = achievements.FirstOrDefault(x =>
                    x.GuildId == guildId &&
                    x.ContextId == contextId &&
                    x.CategoryId == category.Id &&
                    x.Info.Name == categoryAchievementName);
                if (categoryAchievement is null)
                {
                    categoryAchievement = new DiffStarAchievement(
                        id: default,
                        guildId: guildId,
                        contextId: contextId,
                        categoryId: category.Id,
                        info: new AchievementInfo
                        {
                            Name = Name_2_50.CreateUnsafe(categoryAchievementName)
                                .Value,
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
        => legacyLevelNumber == 100 ? null : (uint)Math.Round(legacyLevelNumber);

    private static Expression<Func<RankedMap, bool>> MapDifficultyIsAlreadyRankedOnGuild(
        GuildId guildId, EDifficulty difficulty, BeatSaverKey beatSaverKey, string gameMode,
        ServerDbContext dbContext)
        => rankedMap => rankedMap.GuildId == guildId && rankedMap.MapVersions.Any(y =>
            y.SongDifficulty.GameMode.Name.Contains(gameMode)
            && y.SongDifficulty.Difficulty == difficulty
            && dbContext.Songs.Any(z => z.Id == y.SongId && z.BeatSaverKey == beatSaverKey));
}