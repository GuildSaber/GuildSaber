using GuildSaber.Api.Features.Guilds.Levels.Http;

namespace GuildSaber.Api.Features.Guilds.Members.LevelStats.Http;

public static class LevelStatResponses
{
    public record MemberLevelStat(
        LevelResponses.Level Level,
        bool IsCompleted,
        bool IsLocked,
        int? PassCount
    );
}