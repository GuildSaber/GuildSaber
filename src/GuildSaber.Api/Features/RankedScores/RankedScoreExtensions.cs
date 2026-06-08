using System.Linq.Expressions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Shared;
using GuildSaber.Database.Models.Server.RankedScores;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests;

namespace GuildSaber.Api.Features.RankedScores;

public static class RankedScoreExtensions
{
    public static Expression<Func<RankedScore, bool>> IsValidPassesExpression =>
        x => x.IsSelected && x is PointGivingRankedScore;

    public static Expression<Func<RankedScore, bool>> IsValidOrPendingExpression =>
        x => x.IsSelected && (x is PointGivingRankedScore || x is PendingRankedScore);

    public static IQueryable<RankedScore> ApplyTypeFilter(
        this IQueryable<RankedScore> query, ERankedScoreType rankedScoreTypes)
    {
        if (rankedScoreTypes is ERankedScoreType.None)
            return query;

        var includeValid = rankedScoreTypes.HasFlag(ERankedScoreType.Valid);
        var includeInvalid = rankedScoreTypes.HasFlag(ERankedScoreType.Invalid);
        var includePending = rankedScoreTypes.HasFlag(ERankedScoreType.Pending);
        var includeAccepted = rankedScoreTypes.HasFlag(ERankedScoreType.Accepted);
        var includeRefused = rankedScoreTypes.HasFlag(ERankedScoreType.Refused);

        return query.Where(x =>
            includeValid && x is ValidRankedScore
            || includeInvalid && x is InvalidRankedScore
            || includePending && x is PendingRankedScore
            || includeAccepted && x is AcceptedRankedScore
            || includeRefused && x is RefusedRankedScore);
    }

    public static IQueryable<RankedScore> ApplySortOrder(
        this IQueryable<RankedScore> query, ERankedScoreSorter sortBy, EOrder order) => sortBy switch
    {
        ERankedScoreSorter.Points => query
            .OrderBy(x => x is PointGivingRankedScore ? 1 : 0)
            .ThenBy(order, x => x is ScoredRankedScore ? ((ScoredRankedScore)x).RawPoints : default)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.DifficultyStar => query
            .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
            .ThenBy(order, x => x.RankedMap.Rating.DiffStar)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.AccuracyStar => query
            .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
            .ThenBy(order, x => x.RankedMap.Rating.AccStar)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.Score => query
            .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
            .ThenBy(order, x => x.EffectiveScore)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.Accuracy => query
            .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
            .ThenBy(order, x => x.EffectiveScore / x.SongDifficulty.Stats.MaxScore)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.ScoreTime => query
            .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
            .ThenBy(order, x => x.Score.SetAt)
            .ThenBy(x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}
