using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using AwesomeAssertions;
using AwesomeAssertions.Collections;
using GuildSaber.Common.Services.BeatLeader.Models;

namespace GuildSaber.UnitTests.Utils;

public static class AssertionsUtils
{
    public static AndConstraint<SubsequentOrderingAssertions<T>> BeInOrders<T, TSelector>(
        this GenericCollectionAssertions<T> assertions,
        Expression<Func<T, TSelector>> propertyExpression,
        Order order, [StringSyntax("CompositeFormat")] string because = "",
        params object[] becauseArgs) => order switch
    {
        Order.Desc => assertions.BeInDescendingOrder(propertyExpression, because, becauseArgs),
        Order.Asc => assertions.BeInAscendingOrder(propertyExpression, because, becauseArgs),
        _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
    };
}