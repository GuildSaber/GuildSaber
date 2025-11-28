using System.Linq.Expressions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Database.Models.Server.RankedScores;
using static GuildSaber.Api.Features.RankedScores.RankedScoreRequests;

namespace GuildSaber.Api.Features.RankedScores;

public static class RankedScoreExtensions
{
    extension(RankedScore)
    {
        public static Expression<Func<RankedScore, bool>> IsValidPassesExpression =>
            x => x.State.HasFlag(RankedScore.EState.Selected)
                 && ((int)x.State & (int)RankedScore.EState.NonPointGiving) == 0;
    }

    public static IQueryable<RankedScore> ApplySortOrder(
        this IQueryable<RankedScore> query, ERankedScoreSorter sortBy, EOrder order) => sortBy switch
    {
        ERankedScoreSorter.Points => query
            .OrderBy(x => (x.State & RankedScore.EState.NonPointGiving) != 0 ? 1 : 0)
            .ThenBy(order, x => x.RawPoints)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.DifficultyStar => query
            .OrderBy(x => (x.State & RankedScore.EState.NonPointGiving) != 0 ? 1 : 0)
            .ThenBy(order, x => x.RankedMap.Rating.DiffStar)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.AccuracyStar => query
            .OrderBy(x => (x.State & RankedScore.EState.NonPointGiving) != 0 ? 1 : 0)
            .ThenBy(order, x => x.RankedMap.Rating.AccStar)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.Score => query
            .OrderBy(x => (x.State & RankedScore.EState.NonPointGiving) != 0 ? 1 : 0)
            .ThenBy(order, x => x.EffectiveScore)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.Accuracy => query
            .OrderBy(x => (x.State & RankedScore.EState.NonPointGiving) != 0 ? 1 : 0)
            .ThenBy(order, x => x.EffectiveScore / x.SongDifficulty.Stats.MaxScore)
            .ThenBy(x => x.Id),
        ERankedScoreSorter.ScoreTime => query
            .OrderBy(x => (x.State & RankedScore.EState.NonPointGiving) != 0 ? 1 : 0)
            .ThenBy(order, x => x.Score.SetAt)
            .ThenBy(x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}