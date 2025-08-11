using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HeadLess.SQLBuilder.Base;
using static HeadLess.SQLBuilder.Utils.Helpers;

namespace HeadLess.SQLBuilder.Builders;
public class UpdateBuilder<T> : BaseUpdateDeleteBuilder<UpdateBuilder<T>, T> where T : class
{
    private readonly string _tableName = GetAliasFromTypeName(typeof(T));
    private readonly Dictionary<string, string> _setClauses = new();
    private string Alias => GetAlias(typeof(T));
    private readonly T? _model;

    public UpdateBuilder() 
    {
        _model = null;
    }

    public UpdateBuilder(T? model = null)
    {
        _model = model;
    }

    protected override string GetAlias(Type type) => GetAliasFromTypeName(type);

    private string NextParamName() => $"@p{_paramIndex++}";

    private UpdateBuilder<T> AddSetClause(string? alias, string column, object? value)
    {
        var paramName = NextParamName();
        var aliasPrefix = alias != null ? alias + "." : $"{Alias}.";
        _parameters[paramName] = value;
        _setClauses[$"{aliasPrefix}{column}"] = paramName;
        return this;
    }

    public UpdateBuilder<T> Set(string? alias, string column, object? value)
        => AddSetClause(string.IsNullOrEmpty(alias) ? null : alias, column, value);

    public UpdateBuilder<T> Set<TJoin>(Expression<Func<TJoin, object?>> selector, object? value)
    {
        var alias = GetAliasFromTypeName(typeof(TJoin));
        var column = GetPropertyName(selector);
        return AddSetClause(alias, column, value);
    }

    public UpdateBuilder<T> Set(Expression<Func<T, object?>> selector, object? value)
    {
        var alias = GetAliasFromTypeName(typeof(T));
        var column = GetPropertyName(selector);
        return AddSetClause(alias, column, value);
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

            if (IsSimpleType(prop.PropertyType))
                AddSetClause(null, column, value);
            else
                AddSetClause(null, column, JsonSerializer.Serialize(value));
        }
        return this;
    }

    public (string Sql, Dictionary<string, object> Parameters) Build(string keyColumn = "Id")
    {
        if (!_setClauses.Any())
            throw new InvalidOperationException("No SET clause defined.");

        var aliasKey = $"{_tableName}.{keyColumn}";

        bool hasKeyColumn = _whereClauses.Any(w =>
            Regex.IsMatch(w.Clause, $@"\b{Regex.Escape(keyColumn)}\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(w.Clause, $@"\b{Regex.Escape(aliasKey)}\b", RegexOptions.IgnoreCase)
        );

        if (!hasKeyColumn)
            throw new InvalidOperationException($"WHERE clause must include a condition on '{keyColumn}'.");

        var sb = new StringBuilder();

        if (_joins.Count > 0)
        {
            sb.Append($"UPDATE {_tableName} AS {Alias} ");
            sb.Append(string.Join(" ", _joins) + " ");
        }
        else sb.Append($"UPDATE {_tableName} ");

        sb.Append("SET ");
        sb.Append(string.Join(", ", _setClauses.Select(kv => $"{(_joins.Count > 0 ? kv.Key : kv.Key.Split(".")[1])} = {kv.Value}")));

        if (_whereClauses.Any())
        {
            sb.Append(" WHERE ");
            for (int i = 0; i < _whereClauses.Count; i++)
            {
                if (i > 0) sb.Append($" {_whereClauses[i].Logic} ");
                sb.Append(_whereClauses[i].Clause);
            }
        }

        sb.Append(";");
        return (sb.ToString(), new Dictionary<string, object>(_parameters));
    }
}