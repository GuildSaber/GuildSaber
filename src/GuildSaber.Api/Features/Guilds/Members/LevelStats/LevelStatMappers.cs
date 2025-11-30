using System.Linq.Expressions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Guilds.Members.LevelStats;

public static class LevelStatMappers
{
    public static Expression<Func<Level, LevelStatResponses.Level>> MapLevelExpression
        => level => level.Type == Level.ELevelType.RankedMapList
            ? new LevelStatResponses.Level.RankedMapListLevel(
                level.Id.Value,
                level.CategoryId.HasValue ? level.CategoryId.Value.Value : null,
                new LevelStatResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                (int)level.Order,
                level.IsLocking,
                (int)((RankedMapListLevel)level).RequiredPassCount)
            : level.Type == Level.ELevelType.DiffStar
                ? new LevelStatResponses.Level.DiffStarLevel(
                    level.Id.Value,
                    level.CategoryId.HasValue ? level.CategoryId.Value.Value : null,
                    new LevelStatResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                    (int)level.Order,
                    level.IsLocking,
                    ((DiffStarLevel)level).MinDiffStar,
                    (int)((DiffStarLevel)level).RequiredPassCount)
                : new LevelStatResponses.Level.AccStarLevel(
                    level.Id.Value,
                    level.CategoryId.HasValue ? level.CategoryId.Value.Value : null,
                    new LevelStatResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                    (int)level.Order,
                    level.IsLocking,
                    ((AccStarLevel)level).MinAccStar,
                    (int)((AccStarLevel)level).RequiredPassCount);

    public static Expression<Func<MemberLevelStat, LevelStatResponses.MemberLevelStat>>
        MapMemberLevelStatExpression(ServerDbContext dbContext)
        => self => new LevelStatResponses.MemberLevelStat(
            dbContext.Levels
                .Where(l => l.Id == self.LevelId)
                .Select(MapLevelExpression)
                .First(),
            self.IsCompleted,
            self.IsLocked,
            self.PassCount
        );
}