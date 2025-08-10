using System.Linq.Expressions;
using System.Reflection;

namespace HeadLess.SQLBuilder.Utils;

internal static class Helpers
{
    private static readonly string[] Suffixes = { "Dto", "Entity", "Model", "ViewModel" };

    public static string GetAliasFromTypeName(Type type)
    {
        var name = type.Name;
        foreach (var suffix in Suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - suffix.Length);
        }
        return name;
    }    
    public static string Escape(string input) => input.Replace("'", "''");
    
    public static string GetPropertyName<TProp>(Expression <Func<TProp>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException("Invalid expression");
    }

    public static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression innerMember)
            return innerMember.Member.Name;
        throw new ArgumentException("Invalid expression");
    }    
    
    public static object? GetDefault(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;

    public static string GetColumnName(PropertyInfo prop)
        => prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()?.Name ?? prop.Name;
    
    public static string GetColumnName(string prop) => prop;

    public static bool IsSimpleType(Type type)
        => type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal) || type == typeof(Guid);    
}


