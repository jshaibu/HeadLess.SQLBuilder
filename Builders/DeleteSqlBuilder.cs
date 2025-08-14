using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HeadLess.SQLBuilder.Base;
using static HeadLess.SQLBuilder.Utils.Helpers;

namespace HeadLess.SQLBuilder.Builders;
public class DeleteBuilder<T> : BaseUpdateDeleteBuilder<DeleteBuilder<T>, T> where T : class
{
    private string TableName => GetAliasFromTypeName(typeof(T));
    private string Alias => GetAlias(typeof(T));

    protected override string GetAlias(Type type) => TableName;

    public (string Sql, Dictionary<string, object> Parameters) Build(string keyColumn = "Id")
    {
        var aliasKey = $"{TableName}.{keyColumn}";

        bool hasKeyColumn = _whereClauses.Any(w =>
            Regex.IsMatch(w.Clause, $@"\b{Regex.Escape(keyColumn)}\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(w.Clause, $@"\b{Regex.Escape(aliasKey)}\b", RegexOptions.IgnoreCase)
        );

        if (!hasKeyColumn)
            throw new InvalidOperationException($"DELETE must include a condition on '{keyColumn}'.");

        var sb = new StringBuilder();

        if (_joins.Count > 0)
        {
            sb.Append($"DELETE FROM {TableName} AS { Alias }");
            sb.Append(" " + string.Join(" ", _joins));
        } else sb.Append($"DELETE FROM {TableName}");

        sb.Append(" WHERE ");
        for (int i = 0; i < _whereClauses.Count; i++)
        {
            if (i > 0) sb.Append($" {_whereClauses[i].Logic} ");
            sb.Append((_joins.Count > 0 ? _whereClauses[i].Clause : _whereClauses[i].Clause.Split(".")[1]));
        }

        sb.Append(";");
        return (sb.ToString(), new Dictionary<string, object>(_parameters));
    }
}