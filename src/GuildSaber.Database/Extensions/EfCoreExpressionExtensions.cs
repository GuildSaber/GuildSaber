using System.Linq.Expressions;

namespace GuildSaber.Database.Extensions;

public static class EfCoreExpressionExtensions
{
    /// <summary>
    /// An ExpressionVisitor that replaces one expression with another in the expression tree.
    /// </summary>
    private sealed class ReplaceExpressionVisitor(Expression oldExpression, Expression newExpression)
        : ExpressionVisitor
    {
        /// <summary>
        /// Visits the children of the UnaryExpression.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        public override Expression? Visit(Expression? node)
            => node == oldExpression ? newExpression : base.Visit(node);
    }

    /// <summary>
    /// Combines two expressions with a logical OR operation.
    /// </summary>
    /// <typeparam name="T">The type of the object that the expressions evaluate.</typeparam>
    /// <param name="expr1">The first expression to be combined.</param>
    /// <param name="expr2">The second expression to be combined.</param>
    /// <returns>A new expression that represents the logical OR operation of the two input expressions.</returns>
    /// <remarks>
    /// This method is EF Core compliant and can be used in LINQ queries.
    /// </remarks>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left!, right!), parameter);
    }

    /// <summary>
    /// Combines two expressions with a logical AND operation.
    /// </summary>
    /// <typeparam name="T">The type of the object that the expressions evaluate.</typeparam>
    /// <param name="expr1">The first expression to be combined.</param>
    /// <param name="expr2">The second expression to be combined.</param>
    /// <returns>A new expression that represents the logical AND operation of the two input expressions.</returns>
    /// <remarks>
    /// This method is EF Core compliant and can be used in LINQ queries.
    /// </remarks>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                   Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }
}