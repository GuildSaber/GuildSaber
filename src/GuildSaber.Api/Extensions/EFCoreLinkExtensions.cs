using System.Linq.Expressions;
using GuildSaber.Api.Features.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace GuildSaber.Api.Extensions;

public static class EFCoreLinkExtensions
{
    public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(
        this IQueryable<TSource> source, EOrder by,
        Expression<Func<TSource, TKey>> keySelector)
        => by == EOrder.Desc ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);

    public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(
        this IOrderedQueryable<TSource> source, EOrder by,
        Expression<Func<TSource, TKey>> keySelector)
        => by == EOrder.Desc ? source.ThenByDescending(keySelector) : source.ThenBy(keySelector);

    public static IQueryable<T> If<T>(
        this IQueryable<T> source, bool condition,
        Func<IQueryable<T>, IQueryable<T>> then)
        => condition ? then(source) : source;

    public static IQueryable<T> If<T, P>(
        this IIncludableQueryable<T, P> source, bool condition,
        Func<IIncludableQueryable<T, P>, IQueryable<T>> then) where T : class
        => condition ? then(source) : source;

    public static IQueryable<T> If<T, P>(
        this IIncludableQueryable<T, IEnumerable<P>> source, bool condition,
        Func<IIncludableQueryable<T, IEnumerable<P>>, IQueryable<T>> then) where T : class
        => condition ? then(source) : source;
}