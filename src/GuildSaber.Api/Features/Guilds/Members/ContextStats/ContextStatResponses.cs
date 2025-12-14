namespace GuildSaber.Api.Features.Guilds.Members.ContextStats;

public static class ContextStatResponses
{
    public readonly record struct MemberContextStat(
        SimplePointWithRank[] SimplePointsWithRank,
        PassCountWithRank PassCountWithRank
    );

    public readonly record struct PassCountWithRank(
        int PassCount,
        int Rank
    );

    public readonly record struct SimplePointWithRank(
        int PointId,
        int? CategoryId,
        float Points,
        string Name,
        int Rank
    );
}