using System.Linq.Expressions;

namespace Cg.Template.Domain.Extensions;

public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>>? WhereIf<T>(
        this Expression<Func<T, bool>>? expression,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (!condition)
            return expression;

        if (expression is null)
            return predicate;

        return expression.And(predicate);
    }

    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var parameter = Expression.Parameter(typeof(T), "x");

        var leftBody = ReplaceParameter(
            left.Body,
            left.Parameters[0],
            parameter);

        var rightBody = ReplaceParameter(
            right.Body,
            right.Parameters[0],
            parameter);

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(leftBody, rightBody),
            parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var parameter = Expression.Parameter(typeof(T), "x");

        var leftBody = ReplaceParameter(
            left.Body,
            left.Parameters[0],
            parameter);

        var rightBody = ReplaceParameter(
            right.Body,
            right.Parameters[0],
            parameter);

        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(leftBody, rightBody),
            parameter);
    }

    private static Expression ReplaceParameter(
        Expression expression,
        ParameterExpression source,
        ParameterExpression target)
    {
        return new ParameterReplacer(source, target)
            .Visit(expression)!;
    }

    private sealed class ParameterReplacer(
        ParameterExpression source,
        ParameterExpression target)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return node == source
                ? target
                : base.VisitParameter(node);
        }
    }
}