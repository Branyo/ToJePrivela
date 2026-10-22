using System.Linq.Expressions;

namespace ToJePrivela.Domain.Common;

/// <summary>Combines predicates kept as expressions, so a database query can still translate the result.</summary>
public static class Predicates
{
    /// <summary>Both <paramref name="first"/> and <paramref name="second"/>, as one expression over one parameter.</summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = first.Parameters[0];
        var secondBody = new ParameterReplacer(second.Parameters[0], parameter).Visit(second.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(first.Body, secondBody), parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
