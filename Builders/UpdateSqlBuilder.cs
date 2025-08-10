using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HeadLess.SQLBuilder.Base;
using static HeadLess.SQLBuilder.Utils.Helpers;

namespace HeadLess.SQLBuilder.Builders;

public class UpdateBuilder<T> : WhereClauseBuilderBase<UpdateBuilder<T>, T> where T : class
{
    private readonly string _tableName = GetAliasFromTypeName(typeof(T));
    private readonly Dictionary<string, string> _setClauses = new();
    private readonly T? _model;

    public UpdateBuilder(T? model = null)
    {
        _model = model;
    }

    protected override string GetAlias(Type type)
    {
        // Aliasing generally unnecessary for update
        return _tableName;
    }

    public UpdateBuilder<T> Set(string column, object? value)
    {
        var paramName = $"@p{_paramIndex++}";
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

    // The Where and OrWhere methods are inherited from the base and support expressions fluently
    // You can optionally add overloads for string-based conditions as well, calling AddWhere internally

    public (string Sql, Dictionary<string, object> Parameters) Build(string keyColumn = "Id")
    {
        if (!_setClauses.Any())
            throw new InvalidOperationException("No SET clause defined.");

        // Ensure WHERE contains keyColumn
        if (!_whereClauses.Any(w => w.Clause.Contains($"{keyColumn} ")))
            throw new InvalidOperationException($"WHERE clause must include a condition on the key column '{keyColumn}'.");

        var sb = new StringBuilder();
        sb.Append($"UPDATE {_tableName} SET ");
        sb.Append(string.Join(", ", _setClauses.Select(kv => $"{kv.Key} = {kv.Value}")));

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
