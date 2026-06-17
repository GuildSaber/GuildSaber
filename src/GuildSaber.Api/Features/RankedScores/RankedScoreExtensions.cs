using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.RankedScores;

namespace GuildSaber.Api.Features.RankedScores;

public static class RankedScoreExtensions
{
    public static Expression<Func<RankedScore, bool>> IsValidPassesExpression =>
        x => x.IsSelected && x is PointGivingRankedScore;

    public static Expression<Func<RankedScore, bool>> IsValidOrPendingExpression =>
        x => x.IsSelected && (x is PointGivingRankedScore || x is PendingRankedScore);
}