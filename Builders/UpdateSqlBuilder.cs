using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HeadLess.SQLBuilder;
using HeadLess.SQLBuilder.Base;

namespace HeadLess.SQLBuilder.Builders;
    
public class UpdateBuilder<T> : BaseSqlBuilder<T> where T : class
{
    private readonly string _tableName = typeof(T).Name;
    private readonly Dictionary<string, string> _setClauses = new();
    private readonly T? _model;

    public UpdateBuilder(T? model = null)
    {
        _model = model;
    }

    public UpdateBuilder<T> Set(string column, object? value)
    {
        var paramName = GetParamName();
        _parameters[paramName] = value;
        _setClauses[column] = paramName;
        return this;
    }

    public UpdateBuilder<T> AutoMap()
    {
        if (_model is null) return this;

        foreach (var prop in _model.GetType().GetProperties())
        {
            if (!prop.CanRead) continue;

            var value = prop.GetValue(_model);
            if (value == null || Equals(value, GetDefault(prop.PropertyType))) continue;

            var column = GetColumnName(prop);
            var finalValue = IsSimpleType(prop.PropertyType) ? value : JsonSerializer.Serialize(value);
            Set(column, finalValue);
        }

        return this;
    }

    // Override base Where methods to return UpdateBuilder<T> for fluent chaining
    public new UpdateBuilder<T> Where(string column, string op, object? value)
    {
        base.Where(column, op, value);
        return this;
    }

    public new UpdateBuilder<T> OrWhere(string column, string op, object? value)
    {
        base.OrWhere(column, op, value);
        return this;
    }

    public new UpdateBuilder<T> WhereGroup(Func<BaseSqlBuilder<T>, BaseSqlBuilder<T>> groupBuilder)
    {
        base.WhereGroup(groupBuilder);
        return this;
    }

    public (string, Dictionary<string, object?>) Build(string keyColumn = "Id")
    {
        if (!_setClauses.Any())
            throw new InvalidOperationException("No SET clause defined.");

        if (!_whereClauses.Any(w => w.Clause.StartsWith(keyColumn + " ") || w.Clause.Contains($"{keyColumn} ")))
            throw new InvalidOperationException($"WHERE clause must include a condition on the key column '{keyColumn}'.");

        var sb = new StringBuilder();
        sb.Append($"UPDATE {_tableName} SET ");
        sb.Append(string.Join(", ", _setClauses.Select(kv => $"{kv.Key} = {kv.Value}")));

        if (_whereClauses.Any())
        {
            sb.Append(" WHERE ");
            sb.Append(_whereClauses.Select((c, i) =>
                i == 0 ? c.Clause : $"{c.Logic} {c.Clause}").Aggregate((a, b) => $"{a} {b}"));
        }

        sb.Append(";");

        // Return SQL and parameters dictionary (includes SET and WHERE params)
        return (sb.ToString(), new Dictionary<string, object?>(_parameters));
    }

    protected override BaseSqlBuilder<T> CreateNewBuilder() => new UpdateBuilder<T>(_model);

    private static object? GetDefault(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}

