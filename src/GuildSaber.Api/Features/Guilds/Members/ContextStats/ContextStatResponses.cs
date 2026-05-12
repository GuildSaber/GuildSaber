namespace GuildSaber.Api.Features.Guilds.Members.ContextStats;

public static class ContextStatResponses
{
    public readonly record struct MemberContextStat(
        SimplePointWithRank[] SimplePointsWithRank,
        PassCountWithRank[] PassCountsWithRank
    );

    public readonly record struct PassCountWithRank(
        CategoryId? CategoryId,
        int PassCount,
        int Rank
    );

    public readonly record struct SimplePointWithRank(
        int PointId,
        CategoryId? CategoryId,
        float Points,
        string Name,
        int Rank
    );
}