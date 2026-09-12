using GuildSaber.Api.Features.Guilds.Achievements.Http;

namespace GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http;

public static class AchievementStatResponses
{
    public record MemberAchievementStat(
        AchievementResponses.Achievement Achievement,
        bool IsCompleted,
        bool IsLocked,
        int? PassCount
    );
}