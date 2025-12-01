using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using AwesomeAssertions;
using AwesomeAssertions.Collections;
using GuildSaber.Api.Features.Internal;

namespace GuildSaber.AspireTests.Utils;

public static class AssertionsUtils
{
    public static AndConstraint<SubsequentOrderingAssertions<T>> BeInOrders<T, TSelector>(
        this GenericCollectionAssertions<T> assertions,
        Expression<Func<T, TSelector>> propertyExpression,
        EOrder order, [StringSyntax("CompositeFormat")] string because = "",
        params object[] becauseArgs) => order switch
    {
        EOrder.Desc => assertions.BeInDescendingOrder(propertyExpression, because, becauseArgs),
        EOrder.Asc => assertions.BeInAscendingOrder(propertyExpression, because, becauseArgs),
        _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
    };
}