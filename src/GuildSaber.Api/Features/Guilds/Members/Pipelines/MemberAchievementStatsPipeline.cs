using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;
using AccStarQueryFunc = System.Func<
    GuildSaber.Database.Contexts.Server.ServerDbContext,
    GuildSaber.Common.StrongTypes.GuildId,
    GuildSaber.Common.StrongTypes.ContextId,
    GuildSaber.Common.StrongTypes.PlayerId,
    GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId,
    int?,
    float?,
    float?, System.Threading.Tasks.Task<int>
>;
using DiffStarQueryFunc = System.Func<
    GuildSaber.Database.Contexts.Server.ServerDbContext,
    GuildSaber.Common.StrongTypes.GuildId,
    GuildSaber.Common.StrongTypes.ContextId,
    GuildSaber.Common.StrongTypes.PlayerId,
    GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId,
    int?,
    float?,
    float?, System.Threading.Tasks.Task<int>
>;
using RankedMapListPassCountQueryFunc = System.Func<
    GuildSaber.Database.Contexts.Server.ServerDbContext,
    GuildSaber.Common.StrongTypes.GuildId,
    GuildSaber.Common.StrongTypes.ContextId,
    GuildSaber.Common.StrongTypes.PlayerId,
    GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId,
    GuildSaber.Database.Models.Server.Guilds.Achievements.Achievement.AchievementId,
    int?, System.Threading.Tasks.Task<int>
>;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberAchievementStatsPipeline(
    ServerDbContext dbContext,
    ILogger<MemberAchievementStatsPipeline> logger)
{
    private static readonly AccStarQueryFunc _getAccStarPassCountQuery = EF.CompileAsyncQuery((
        ServerDbContext db, GuildId guildId, ContextId contextId, PlayerId playerId,
        Point.PointId pointId, int? categoryId,
        float? minStar, float? maxStar) => db.RankedScores
        .Where(x =>
            x.GuildId == guildId &&
            x.ContextId == contextId &&
            x.PlayerId == playerId &&
            x.PointId == pointId)
        .Where(RankedScoreExtensions.IsValidPassesExpression)
        .Where(x => minStar == null || x.RankedMap.Rating.AccStar >= minStar.Value)
        .Where(x => maxStar == null || x.RankedMap.Rating.AccStar < maxStar.Value)
        .Count(x => categoryId == null || x.RankedMap.Categories.Any(c => c.Id == categoryId.Value)));

    private static readonly DiffStarQueryFunc _getDiffStarPassCountQuery = EF.CompileAsyncQuery((
        ServerDbContext db, GuildId guildId, ContextId contextId, PlayerId playerId,
        Point.PointId pointId, int? categoryId,
        float? minStar, float? maxStar) => db.RankedScores
        .Where(x =>
            x.GuildId == guildId &&
            x.ContextId == contextId &&
            x.PlayerId == playerId &&
            x.PointId == pointId)
        .Where(RankedScoreExtensions.IsValidPassesExpression)
        .Where(x => minStar == null || x.RankedMap.Rating.DiffStar >= minStar.Value)
        .Where(x => maxStar == null || x.RankedMap.Rating.DiffStar < maxStar.Value)
        .Count(x => categoryId == null || x.RankedMap.Categories.Any(c => c.Id == categoryId.Value)));

    private static readonly RankedMapListPassCountQueryFunc _getRankedMapListPassCountQuery = EF.CompileAsyncQuery((
        ServerDbContext db, GuildId guildId, ContextId contextId, PlayerId playerId,
        Point.PointId pointId, Achievement.AchievementId achievementId, int? categoryId) => db.RankedScores
        .Where(x =>
            x.GuildId == guildId &&
            x.ContextId == contextId &&
            x.PlayerId == playerId &&
            x.PointId == pointId)
        .Where(RankedScoreExtensions.IsValidPassesExpression)
        .Where(x => x.RankedMap.Achievements.Any(achievement => achievement.Id == achievementId))
        .Count(x => categoryId == null || x.RankedMap.Categories.Any(c => c.Id == categoryId.Value)));

    /// <summary>
    /// Recalculates achievement stats for a specific player in a specific context.
    /// </summary>
    /// <param name="playerId">The player to recalculate achievement stats for.</param>
    /// <param name="guildId">The guild the context belongs to.</param>
    /// <param name="contextId">The context to recalculate achievement stats for.</param>
    /// <param name="pointId">
    /// The point is simply used for more efficient querying (since the passes are the same on multiple context points.
    /// </param>
    [SuppressMessage("ReSharper", "InvertIf")]
    public async Task ExecuteAsync(PlayerId playerId, GuildId guildId, ContextId contextId, Point.PointId pointId)
    {
        logger.LogInformation("Recalculating member achievement stats for player {PlayerId} in context {ContextId}",
            playerId, contextId);
        var (achievements, achievementStats) = (
            await dbContext.Achievements
                .Where(x => x.ContextId == contextId)
                .ToArrayAsync(),
            await dbContext.MemberAchievementStats
                .AsTracking()
                .Where(x => x.PlayerId == playerId && x.ContextId == contextId)
                .ToDictionaryAsync(x => x.AchievementId)
        );

        foreach (var achievement in achievements)
        {
            ref var dictionaryValue = ref CollectionsMarshal.GetValueRefOrAddDefault(achievementStats, achievement.Id,
                out var exists);
            if (exists) continue;

            dictionaryValue = new MemberAchievementStat
            {
                GuildId = guildId,
                ContextId = contextId,
                PlayerId = playerId,
                AchievementId = achievement.Id
            };

            dbContext.MemberAchievementStats.Add(dictionaryValue);
        }

        var isLocked = new Dictionary<CategoryId, bool>();
        foreach (var achievement in achievements
                     .GroupBy(x => x.CategoryId)
                     .SelectMany(x => x
                         .OrderBy(y => y.ProgressionOrder is null)
                         .ThenBy(y => y.ProgressionOrder)))
        {
            var achievementStat = achievementStats[achievement.Id];
            await RecalculateMemberAchievementStat(achievementStat, achievement, pointId);

            if (achievement.ProgressionOrder is null)
                continue;

            if (isLocked.TryGetValue(achievement.CategoryId ?? default, out var blocked) && blocked)
                achievementStat.IsLocked = true;

            if (achievement.IsLocking && !achievementStat.IsCompleted)
            {
                achievementStat.IsLocked = true;
                isLocked[achievement.CategoryId ?? default] = true;
            }
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation(
            "Completed recalculating member achievement stats for player {PlayerId} in context {ContextId}",
            playerId, contextId);
    }

    public ValueTask RecalculateMemberAchievementStat(
        MemberAchievementStat memberAchievementStat, Achievement achievement, Point.PointId pointId)
        => achievement switch
        {
            RankedMapListAchievement rankedMapListAchievement => RecalculateRankedMapListAchievementStat(
                memberAchievementStat, rankedMapListAchievement, achievement.CategoryId, pointId),
            DiffStarAchievement diffStarAchievement => RecalculateDiffStarAchievementStat(
                memberAchievementStat, diffStarAchievement, achievement.CategoryId, pointId),
            AccStarAchievement accStarAchievement => RecalculateAccStarAchievementStat(
                memberAchievementStat, accStarAchievement, achievement.CategoryId, pointId),
            _ => throw new UnreachableException($"Unknown achievement type: {achievement.GetType().FullName}")
        };

    public async ValueTask RecalculateRankedMapListAchievementStat(
        MemberAchievementStat memberAchievementStat, RankedMapListAchievement achievement,
        CategoryId? categoryId,
        Point.PointId pointId)
    {
        memberAchievementStat.IsLocked = false;
        memberAchievementStat.PassCount = await _getRankedMapListPassCountQuery(
            dbContext,
            memberAchievementStat.GuildId,
            memberAchievementStat.ContextId,
            memberAchievementStat.PlayerId,
            pointId,
            memberAchievementStat.AchievementId,
            categoryId
        );
        memberAchievementStat.IsCompleted = memberAchievementStat.PassCount >= achievement.RequiredPassCount;
    }

    public async ValueTask RecalculateDiffStarAchievementStat(
        MemberAchievementStat memberAchievementStat, DiffStarAchievement achievement,
        CategoryId? categoryId, Point.PointId pointId)
    {
        memberAchievementStat.IsLocked = false;
        memberAchievementStat.PassCount = await _getDiffStarPassCountQuery(
            dbContext,
            memberAchievementStat.GuildId,
            memberAchievementStat.ContextId,
            memberAchievementStat.PlayerId,
            pointId,
            categoryId,
            achievement.MinStar,
            achievement.MaxStar?.Value
        );
        memberAchievementStat.IsCompleted = memberAchievementStat.PassCount >= achievement.RequiredPassCount;
    }

    public async ValueTask RecalculateAccStarAchievementStat(
        MemberAchievementStat memberAchievementStat, AccStarAchievement achievement,
        CategoryId? categoryId, Point.PointId pointId)
    {
        memberAchievementStat.IsLocked = false;
        memberAchievementStat.PassCount = await _getAccStarPassCountQuery(
            dbContext,
            memberAchievementStat.GuildId,
            memberAchievementStat.ContextId,
            memberAchievementStat.PlayerId,
            pointId,
            categoryId,
            achievement.MinStar,
            achievement.MaxStar?.Value
        );
        memberAchievementStat.IsCompleted = memberAchievementStat.PassCount >= achievement.RequiredPassCount;
    }
}