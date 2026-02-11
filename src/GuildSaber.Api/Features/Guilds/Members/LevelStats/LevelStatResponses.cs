using GuildSaber.Api.Features.Guilds.Levels;

namespace GuildSaber.Api.Features.Guilds.Members.LevelStats;

public static class LevelStatResponses
{
    public record MemberLevelStat(
        LevelResponses.Level Level,
        bool IsCompleted,
        bool IsLocked,
        int? PassCount
    );
}