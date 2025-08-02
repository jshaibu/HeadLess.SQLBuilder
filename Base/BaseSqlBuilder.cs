using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;

namespace HeadLess.SQLBuilder.Base;

public abstract class BaseSqlBuilder<T> where T : class
{
    protected readonly Dictionary<string, object?> _parameters = new();
    protected readonly List<(string Logic, string Clause)> _whereClauses = new();
    protected int _paramIndex;

    protected string GetParamName()
        => $"@p{_paramIndex++}";

    protected string FormatValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            _ => value.ToString()!
        };
    }

    protected string GetColumnName(PropertyInfo prop)
    {
        return prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
    }

    protected bool IsSimpleType(Type type) =>
        type.IsPrimitive ||
        type == typeof(string) ||
        type == typeof(DateTime) ||
        type == typeof(decimal) ||
        type == typeof(Guid);

    public BaseSqlBuilder<T> Where(string column, string op, object? value)
        => AddWhere("AND", column, op, value);

    public BaseSqlBuilder<T> OrWhere(string column, string op, object? value)
        => AddWhere("OR", column, op, value);

    private BaseSqlBuilder<T> AddWhere(string logic, string column, string op, object? value)
    {
        var paramName = GetParamName();
        _parameters[paramName] = value;
        _whereClauses.Add((logic, $"{column} {op} {paramName}"));
        return this;
    }

    public BaseSqlBuilder<T> WhereGroup(Func<BaseSqlBuilder<T>, BaseSqlBuilder<T>> groupBuilder)
    {
        var temp = CreateNewBuilder();
        temp._paramIndex = _paramIndex;
        groupBuilder(temp);

        if (!temp._whereClauses.Any()) return this;

        var groupedSql = "(" + temp._whereClauses[0].Clause;
        foreach (var clause in temp._whereClauses.Skip(1))
        {
            groupedSql += $" {clause.Logic} {clause.Clause}";
        }
        groupedSql += ")";

        _whereClauses.Add(("AND", groupedSql));

        foreach (var kvp in temp._parameters)
            _parameters[kvp.Key] = kvp.Value;

        _paramIndex = temp._paramIndex;
        return this;
    }

    protected abstract BaseSqlBuilder<T> CreateNewBuilder();

    public IReadOnlyDictionary<string, object?> Parameters => _parameters;
}


