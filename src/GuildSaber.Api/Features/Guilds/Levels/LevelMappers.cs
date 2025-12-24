using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.Guilds.Levels;

namespace GuildSaber.Api.Features.Guilds.Levels;

public class LevelMappers
{
    public static Expression<Func<Level, LevelResponses.Level>> MapLevelExpression
        => level => level.Type == Level.ELevelType.RankedMapList
            ? new LevelResponses.Level.RankedMapListLevel(
                level.Id.Value,
                level.GuildId,
                level.ContextId,
                level.CategoryId.HasValue ? level.CategoryId.Value.Value : null,
                new LevelResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                level.Order,
                level.IsLocking,
                ((RankedMapListLevel)level).RequiredPassCount,
                ((RankedMapListLevel)level).RankedMaps.Count)
            : level.Type == Level.ELevelType.DiffStar
                ? new LevelResponses.Level.DiffStarLevel(
                    level.Id.Value,
                    level.GuildId,
                    level.ContextId,
                    level.CategoryId.HasValue ? level.CategoryId.Value.Value : null,
                    new LevelResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                    level.Order,
                    level.IsLocking,
                    ((DiffStarLevel)level).MinStar,
                    ((DiffStarLevel)level).RequiredPassCount)
                : new LevelResponses.Level.AccStarLevel(
                    level.Id.Value,
                    level.GuildId,
                    level.ContextId,
                    level.CategoryId.HasValue ? level.CategoryId.Value.Value : null,
                    new LevelResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                    level.Order,
                    level.IsLocking,
                    ((AccStarLevel)level).MinStar,
                    ((AccStarLevel)level).RequiredPassCount);
}