using System.Diagnostics;
using System.Runtime.InteropServices;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using AccStarQueryFunc = System.Func<
    GuildSaber.Database.Contexts.Server.ServerDbContext,
    GuildSaber.Common.StrongTypes.GuildId,
    GuildSaber.Common.StrongTypes.ContextId,
    GuildSaber.Database.Models.Server.Players.Player.PlayerId,
    GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId,
    int?,
    GuildSaber.Database.Models.Server.RankedMaps.RankedMapRating.AccuracyStar?,
    int, System.Threading.Tasks.Task<bool>
>;
using DiffStarQueryFunc = System.Func<
    GuildSaber.Database.Contexts.Server.ServerDbContext,
    GuildSaber.Common.StrongTypes.GuildId,
    GuildSaber.Common.StrongTypes.ContextId,
    GuildSaber.Database.Models.Server.Players.Player.PlayerId,
    GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId,
    int?,
    GuildSaber.Database.Models.Server.RankedMaps.RankedMapRating.DifficultyStar?,
    int, System.Threading.Tasks.Task<bool>
>;
using RankedMapListPassCountQueryFunc = System.Func<
    GuildSaber.Database.Contexts.Server.ServerDbContext,
    GuildSaber.Common.StrongTypes.GuildId,
    GuildSaber.Common.StrongTypes.ContextId,
    GuildSaber.Database.Models.Server.Players.Player.PlayerId,
    GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId,
    GuildSaber.Database.Models.Server.Guilds.Levels.Level.LevelId,
    int?, System.Threading.Tasks.Task<int>
>;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberLevelStatsPipeline(ServerDbContext dbContext)
{
    private static readonly AccStarQueryFunc _checkAccStarCompletionQuery = EF.CompileAsyncQuery((
        ServerDbContext db, GuildId guildId, ContextId contextId, PlayerId playerId,
        Point.PointId pointId, int? categoryId,
        RankedMapRating.AccuracyStar? minAccStar, int skipCount) => db.RankedScores
        .Where(x =>
            x.GuildId == guildId &&
            x.ContextId == contextId &&
            x.PlayerId == playerId &&
            x.PointId == pointId)
        .Where(RankedScoreExtensions.IsValidPassesExpression)
        .Where(x => categoryId == null || x.RankedMap.Categories.Any(c => c.Id == categoryId.Value))
        .Where(x => minAccStar == null || x.RankedMap.Rating.AccStar >= minAccStar.Value)
        .Skip(skipCount)
        .Any());

    private static readonly DiffStarQueryFunc _checkDiffStarCompletionQuery = EF.CompileAsyncQuery((
        ServerDbContext db, GuildId guildId, ContextId contextId, PlayerId playerId,
        Point.PointId pointId, int? categoryId,
        RankedMapRating.DifficultyStar? minAccStar, int skipCount) => db.RankedScores
        .Where(x =>
            x.GuildId == guildId &&
            x.ContextId == contextId &&
            x.PlayerId == playerId &&
            x.PointId == pointId)
        .Where(RankedScoreExtensions.IsValidPassesExpression)
        .Where(x => categoryId == null || x.RankedMap.Categories.Any(c => c.Id == categoryId.Value))
        .Where(x => minAccStar == null || x.RankedMap.Rating.DiffStar >= minAccStar.Value)
        .Skip(skipCount)
        .Any());

    private static readonly RankedMapListPassCountQueryFunc _getRankedMapListPassCountQuery = EF.CompileAsyncQuery((
        ServerDbContext db, GuildId guildId, ContextId contextId, PlayerId playerId,
        Point.PointId pointId, Level.LevelId levelId, int? categoryId) => db.RankedScores
        .Where(x =>
            x.GuildId == guildId &&
            x.ContextId == contextId &&
            x.PlayerId == playerId &&
            x.PointId == pointId)
        .Where(RankedScoreExtensions.IsValidPassesExpression)
        .Where(x => x.RankedMap.Levels.Any(l => l.Id == levelId))
        .Count(x => categoryId == null || x.RankedMap.Categories.Any(c => c.Id == categoryId.Value)));

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
        var (levels, levelsStats) = (
            await dbContext.Levels
                .Where(x => x.ContextId == contextId)
                .ToArrayAsync(),
            await dbContext.MemberLevelStats
                .AsTracking()
                .Where(x => x.PlayerId == playerId && x.ContextId == contextId)
                .ToDictionaryAsync(x => x.LevelId)
        );

        foreach (var level in levels)
        {
            ref var dictionaryValue = ref CollectionsMarshal.GetValueRefOrAddDefault(levelsStats, level.Id,
                out var exists);
            if (exists) continue;

            dictionaryValue = new MemberLevelStat
            {
                GuildId = guildId,
                ContextId = contextId,
                PlayerId = playerId,
                LevelId = level.Id
            };

            dbContext.MemberLevelStats.Add(dictionaryValue);
        }

        var isLocked = new Dictionary<Category.CategoryId, bool>();
        foreach (var level in levels
                     .GroupBy(x => x.CategoryId)
                     .SelectMany(x => x.OrderBy(y => y.Order)))
        {
            var levelStat = levelsStats[level.Id];
            await RecalculateMemberLevelStat(levelStat, level, pointId);

            if (isLocked.TryGetValue(level.CategoryId ?? default, out var blocked) && blocked)
                levelStat.IsLocked = true;

            if (level.IsLocking && !levelStat.IsCompleted)
                isLocked[level.CategoryId ?? default] = true;
        }

        await dbContext.SaveChangesAsync();
    }

    public ValueTask RecalculateMemberLevelStat(
        MemberLevelStat memberLevelStat, Level level, Point.PointId pointId) => level switch
    {
        RankedMapListLevel rankedMapListLevel => RecalculateRankedMapListLevelStat(
            memberLevelStat, rankedMapListLevel, pointId),
        DiffStarLevel diffStarLevel => RecalculateDiffStarLevelStat(
            memberLevelStat, diffStarLevel, pointId),
        AccStarLevel accStarLevel => RecalculateAccStarLevelStat(
            memberLevelStat, accStarLevel, pointId),
        _ => throw new UnreachableException($"Unknown level type: {level.GetType().FullName}")
    };

    public async ValueTask RecalculateRankedMapListLevelStat(
        MemberLevelStat memberLevelStat, RankedMapListLevel level, Point.PointId pointId)
    {
        memberLevelStat.IsLocked = false;
        memberLevelStat.PassCount = await _getRankedMapListPassCountQuery(
            dbContext,
            memberLevelStat.GuildId,
            memberLevelStat.ContextId,
            memberLevelStat.PlayerId,
            pointId,
            level.Id,
            level.CategoryId
        );
        memberLevelStat.IsCompleted = memberLevelStat.PassCount >= level.RequiredPassCount;
    }

    public async ValueTask RecalculateDiffStarLevelStat(
        MemberLevelStat memberLevelStat, DiffStarLevel level, Point.PointId pointId)
    {
        memberLevelStat.IsLocked = false;
        memberLevelStat.PassCount = null;
        memberLevelStat.IsCompleted = await _checkDiffStarCompletionQuery(
            dbContext,
            memberLevelStat.GuildId,
            memberLevelStat.ContextId,
            memberLevelStat.PlayerId,
            pointId,
            level.CategoryId,
            level.MinStar,
            (int)level.RequiredPassCount - 1
        );
    }

    public async ValueTask RecalculateAccStarLevelStat(
        MemberLevelStat memberLevelStat, AccStarLevel level, Point.PointId pointId)
    {
        memberLevelStat.IsLocked = false;
        memberLevelStat.PassCount = null;
        memberLevelStat.IsCompleted = await _checkAccStarCompletionQuery(
            dbContext,
            memberLevelStat.GuildId,
            memberLevelStat.ContextId,
            memberLevelStat.PlayerId,
            pointId,
            level.CategoryId,
            level.MinStar,
            (int)level.RequiredPassCount - 1
        );
    }
}