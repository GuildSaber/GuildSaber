using System.Diagnostics;
using System.Drawing;
using System.Linq.Expressions;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedMaps.MapVersions;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.LegacyGuildSaber;
using GuildSaber.Common.Services.LegacyGuildSaber.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
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
    ILogger<LegacyGuildSaberMapImportPipeline> logger)
{
    private readonly record struct LegacyLevelKey(LegacyLevelId LevelId, LegacyCategoryId CategoryId);

    /// <remarks>
    /// Importing guild with levels being floating points is currently unsupported.
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
        var levelDict = await SyncLevelsWithLegacyAsync(guildId, contextId, categoryDict, token);

        await foreach (var guildRankedMapsResult in legacyGuildSaberApi.GetGuildRankedMaps(guildId.Value, request)
                           .WithCancellation(token))
        {
            if (!guildRankedMapsResult.TryGetValue(out var guildRankedMaps))
                break;

            foreach (var rankedMap in guildRankedMaps.Where(x => x.BeatSaverId is not null))
            foreach (var difficulty in rankedMap.Difficulties.Where(x => x.GameModeName is not null))
            {
                var existingRankedMap = await dbContext.RankedMaps
                    .Where(MapDifficultyIsAlreadyRankedOnGuild(
                        guildId, difficulty.BeatSaverDifficultyValue, rankedMap.BeatSaverId!.Value,
                        difficulty.GameModeName!, dbContext))
                    .ToArrayAsync(token);

                var levelKey = new LegacyLevelKey(difficulty.LevelId,
                    difficulty.GuildCategoryId ?? new LegacyCategoryId(0));
                var level = levelDict[levelKey];

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
                    DifficultyStar: level.Order,
                    AccuracyStar: null);
                int[] categoryIds = difficulty.GuildCategoryId.HasValue && difficulty.GuildCategoryId != 0
                    ? [categoryDict[difficulty.GuildCategoryId.Value].Id]
                    : [];
                int[] levelIds = difficulty.GuildCategoryId.HasValue && difficulty.GuildCategoryId != 0
                    ? [level.Id, levelDict[levelKey].Id]
                    : [level.Id];

                if (existingRankedMap.Length > 0)
                {
                    if (MapShouldBeUpdated())
                        _ = await UpdateMapAsync(contextId, new RankedMapRequests.UpdateRankedMap(
                            Requirements: requirements,
                            ManualRating: manualRating,
                            CategoryIds: categoryIds,
                            LevelIds: levelIds
                        ));

                    continue;
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
                    LevelIds: levelIds
                ), retryCount: 3, token);
            }
        }
    }

    public bool MapShouldBeUpdated() => false;

    public Task<bool> UpdateMapAsync(ContextId contextId, RankedMapRequests.UpdateRankedMap request)
        => throw new NotImplementedException();

    public async Task<bool> RankMapWithRetryAsync(
        ContextId contextId, RankedMapRequests.CreateRankedMap request,
        int retryCount, CancellationToken token)
    {
        do
        {
            var createResult = await rankedMapService.CreateRankedMap(contextId, request);
            switch (createResult)
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
        if (!(await legacyGuildSaberApi.GetRankingCategoriesAsync(guildId.Value)).TryGetValue(out var legacyCategories))
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

    private async Task<Dictionary<LegacyLevelKey, RankedMapListLevel>> SyncLevelsWithLegacyAsync(
        GuildId guildId, ContextId contextId, Dictionary<LegacyCategoryId, Category> categories,
        CancellationToken token)
    {
        var levels = await dbContext.Levels
            .OfType<RankedMapListLevel>()
            .Where(x => x.GuildId == guildId && x.ContextId == contextId)
            .ToListAsync(token);
        if (!(await legacyGuildSaberApi.GetRankingLevelsAsync(guildId.Value)).TryGetValue(out var legacyLevels))
            return [];

        var result = new Dictionary<LegacyLevelKey, RankedMapListLevel>();
        foreach (var legacyLevel in legacyLevels)
        {
            var levelName = $"Lvl {legacyLevel.LevelNumber:G}";
            var level = levels.FirstOrDefault(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.CategoryId == null &&
                x.Info.Name == levelName);
            if (level is null)
            {
                level = new RankedMapListLevel
                {
                    GuildId = guildId,
                    ContextId = contextId,
                    CategoryId = null,
                    Info = new LevelInfo
                    {
                        Name = Name_2_50.CreateUnsafe(levelName).Value,
                        Color = Color.FromArgb(legacyLevel.Color)
                    },
                    Order = (uint)Math.Round(legacyLevel.LevelNumber),
                    IsLocking = true,
                    RequiredPassCount = 1
                };

                dbContext.Levels.Add(level);
            }

            result[new LegacyLevelKey(legacyLevel.Id, new LegacyCategoryId(0))] = level;

            foreach (var (legacyCategoryId, category) in categories)
            {
                var categoryLevelName = $"Lvl {legacyLevel.LevelNumber:G}";
                var categoryLevel = levels.FirstOrDefault(x =>
                    x.GuildId == guildId &&
                    x.ContextId == contextId &&
                    x.CategoryId == category.Id &&
                    x.Info.Name == categoryLevelName);
                if (categoryLevel is null)
                {
                    categoryLevel = new RankedMapListLevel
                    {
                        GuildId = guildId,
                        ContextId = contextId,
                        CategoryId = category.Id,
                        Info = new LevelInfo
                        {
                            Name = Name_2_50.CreateUnsafe(categoryLevelName)
                                .Value,
                            Color = Color.FromArgb(legacyLevel.Color)
                        },
                        Order = (uint)Math.Round(legacyLevel.LevelNumber),
                        IsLocking = true,
                        RequiredPassCount = 1
                    };

                    dbContext.Levels.Add(categoryLevel);
                }
                
                result[new LegacyLevelKey(legacyLevel.Id, legacyCategoryId)] = categoryLevel;
            }
        }

        await dbContext.SaveChangesAsync(token);
        dbContext.ChangeTracker.Clear();

        return result;
    }

    private static Expression<Func<RankedMap, bool>> MapDifficultyIsAlreadyRankedOnGuild(
        GuildId guildId, EDifficulty difficulty, BeatSaverKey beatSaverKey, string gameMode,
        ServerDbContext dbContext)
        => rankedMap => rankedMap.GuildId == guildId && rankedMap.MapVersions.Any(y =>
            y.SongDifficulty.GameMode.Name.Contains(gameMode)
            && y.SongDifficulty.Difficulty == difficulty
            && dbContext.Songs.Any(z => z.Id == y.SongId && z.BeatSaverKey == beatSaverKey));
}