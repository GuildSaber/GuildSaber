using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberLevelStatsPipeline(ServerDbContext dbContext)
{
    /// <summary>
    /// Recalculates level stats for a specific player in a specific context.
    /// </summary>
    /// <param name="playerId">The player to recalculate level stats for.</param>
    /// <param name="guildId">The guild the context belongs to.</param>
    /// <param name="contextId">The context to recalculate level stats for.</param>
    /// <param name="pointId">
    /// The point is simply used for more efficient querying (since the passes are the same on multiple context points.
    /// </param>
    public async Task ExecuteAsync(PlayerId playerId, GuildId guildId, ContextId contextId, Point.PointId pointId)
    {
        var (levels, levelsStats, categories) = (
            await dbContext.Levels
                .Where(x => x.ContextId == contextId)
                .ToDictionaryAsync(x => x.Id),
            await dbContext.MemberLevelStats
                .AsTracking()
                .Where(x => x.PlayerId == playerId && x.ContextId == contextId)
                .ToDictionaryAsync(x => (x.LevelId, x.CategoryId)),
            await dbContext.Categories
                .Where(x => x.GuildId == guildId)
                .ToDictionaryAsync(x => x.Id)
        );

        var levelToProcess = new Dictionary<(Level.LevelId, Category.CategoryId?), Level>();
        // First, add all explicitly defined levels.
        foreach (var level in levels)
        {
            var (levelId, categoryId) = level.Value switch
            {
                GlobalLevel => (level.Key, null as Category.CategoryId?),
                CategoryLevel { CategoryId: var x } => (level.Key, x),
                CategoryLevelOverride { BaseLevelId: var x, CategoryId: var y } => (x, y),
                _ => ThrowHelper.ThrowInvalidOperationException<(Level.LevelId, Category.CategoryId?)>(
                    "Unknown level type.")
            };

            levelToProcess.Add((levelId, categoryId), level.Value);
        }

        // Then, add all global levels that are not ignored in categories.
        foreach (var level in levels.Where(x => x.Value is not GlobalLevel { IsIgnoredInCategories: true }))
        foreach (var key in categories.Select(category => (level.Key, category.Key)))
        {
            ref var dictionaryValue = ref CollectionsMarshal.GetValueRefOrAddDefault(levelToProcess, key,
                out var exists);
            if (exists) continue;

            dictionaryValue = levels[key.Item1];
        }

        // Now, ensure all level stats exist.
        foreach (var (key, _) in levelToProcess)
        {
            ref var dictionaryValue = ref CollectionsMarshal.GetValueRefOrAddDefault(levelsStats, key,
                out var exists);
            if (exists) continue;

            dictionaryValue = new MemberLevelStat
            {
                GuildId = guildId,
                ContextId = contextId,
                PlayerId = playerId,
                LevelId = key.Item1,
                CategoryId = key.Item2
            };

            dbContext.MemberLevelStats.Add(dictionaryValue);
        }

        var validQuery = dbContext.RankedScores
            .Where(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.PlayerId == playerId &&
                x.PointId == pointId)
            .Where(RankedScore.IsValidPassesExpression);

        var (passedMapsDiffDetailsGlobal, passedMapsDiffDetailsPerCategory) = (
            await validQuery
                .Select(x => x.RankedMap.Rating)
                .ToArrayAsync(),
            await validQuery
                .SelectMany(x => x.RankedMap.Categories.Select(y => y.Id),
                    (score, categoryId) => new
                    {
                        CategoryId = categoryId,
                        score.RankedMap.Rating
                    })
                .GroupBy(x => x.CategoryId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Rating).ToArray())
        );

        var isLocked = new Dictionary<Category.CategoryId, bool>();
        // Finally, recalculate all level stats in the correct order.
        foreach (var (key, level) in levelToProcess.OrderBy(x => x.Value.Order))
        {
            var categoryId = key.Item2;
            var levelStat = levelsStats[key];
            var passedMapsDiffDetails = categoryId is null
                ? passedMapsDiffDetailsGlobal
                : passedMapsDiffDetailsPerCategory[categoryId.Value];

            RecalculateLevelStatCompletion(levelStat, level.Requirement, passedMapsDiffDetails);
            if (isLocked.TryGetValue(categoryId ?? default, out var blocked) && blocked)
                levelStat.IsLocked = true;

            if (level.NeedCompletion && !levelStat.IsCompleted)
                isLocked[categoryId ?? default] = true;
        }

        await dbContext.SaveChangesAsync();
    }

    public static void RecalculateLevelStatCompletion(
        MemberLevelStat levelStat, LevelRequirement requirement, RankedMapRating[] passedMapsDiffDetails)
    {
        switch (requirement.Type)
        {
            case LevelRequirement.ELevelRequirementType.DiffStar:
                var minDiffStar = requirement.MinDiffStar;
                var actualPasses = passedMapsDiffDetails.Count(x => x.DiffStar >= minDiffStar);

                levelStat.PassCount = actualPasses;
                levelStat.IsCompleted = actualPasses >= requirement.MinPassCount;
                break;
            case LevelRequirement.ELevelRequirementType.AccStar:
                var minAccStar = requirement.MinAccStar;
                actualPasses = passedMapsDiffDetails.Count(x => x.AccStar >= minAccStar);

                levelStat.PassCount = actualPasses;
                levelStat.IsCompleted = actualPasses >= requirement.MinPassCount;
                break;
            default:
                ThrowHelper.ThrowInvalidOperationException($"Unknown level requirement type: {requirement.Type}");
                break;
        }
    }
}