using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HeadLess.SQLBuilder.Utils;

internal static class WhereClauseUtils
{
    public static string ExtractColumnName(Expression expr)
    {
        if (expr is MemberExpression member)
            return member.Member.Name;

        if (expr is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            return unaryMember.Member.Name;

        throw new InvalidOperationException("Could not extract column name from expression.");
    }

    public static object ExtractValue(Expression expr)
    {
        switch (expr)
        {
            case ConstantExpression constant:
                return constant.Value;

            case MemberExpression member when member.Expression is ConstantExpression:
                var obj = ((ConstantExpression)member.Expression).Value;
                var field = member.Member as FieldInfo;
                return field?.GetValue(obj);

            case UnaryExpression unary when unary.Operand is MemberExpression me:
                return ExtractValue(me);

            case MethodCallExpression methodCall:
                var lambda = Expression.Lambda(methodCall);
                var compiled = lambda.Compile();
                return compiled.DynamicInvoke();

            default:
                var compiledLambda = Expression.Lambda(expr).Compile();
                return compiledLambda.DynamicInvoke();
        }
    }

    public static string GetSqlOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Operator {nodeType} not supported")
        };
    }
}