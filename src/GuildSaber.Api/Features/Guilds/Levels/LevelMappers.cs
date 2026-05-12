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
                level.CategoryId,
                new LevelResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                new LevelResponses.LevelDiscordInfo(level.DiscordInfo.RoleId),
                level.Order,
                level.IsLocking,
                ((RankedMapListLevel)level).RequiredPassCount,
                ((RankedMapListLevel)level).RankedMaps.Count)
            : level.Type == Level.ELevelType.DiffStar
                ? new LevelResponses.Level.DiffStarLevel(
                    level.Id.Value,
                    level.GuildId,
                    level.ContextId,
                    level.CategoryId,
                    new LevelResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                    new LevelResponses.LevelDiscordInfo(level.DiscordInfo.RoleId),
                    level.Order,
                    level.IsLocking,
                    ((DiffStarLevel)level).MinStar,
                    ((DiffStarLevel)level).RequiredPassCount)
                : new LevelResponses.Level.AccStarLevel(
                    level.Id.Value,
                    level.GuildId,
                    level.ContextId,
                    level.CategoryId,
                    new LevelResponses.LevelInfo(level.Info.Name, level.Info.Color.ToArgb()),
                    new LevelResponses.LevelDiscordInfo(level.DiscordInfo.RoleId),
                    level.Order,
                    level.IsLocking,
                    ((AccStarLevel)level).MinStar,
                    ((AccStarLevel)level).RequiredPassCount);
}