using System.Text;
using HeadLess.SQLBuilder.Base;
using static HeadLess.SQLBuilder.Utils.Helpers;

namespace HeadLess.SQLBuilder.Builders;

public class DeleteBuilder<T> : BaseSqlBuilder<DeleteBuilder<T>, T> where T : class
{
    private string TableName => GetAliasFromTypeName(typeof(T));

    protected override string GetAlias(Type type)
    {
        return TableName;
    }

    public (string Sql, Dictionary<string, object> Parameters) Build()
    {
        var sb = new StringBuilder();
        sb.Append($"DELETE FROM {TableName}");

        if (_whereClauses.Count > 0)
        {
            sb.Append(" WHERE ");
            for (int i = 0; i < _whereClauses.Count; i++)
            {
                if (i > 0)
                    sb.Append($" {_whereClauses[i].Logic} ");
                sb.Append(_whereClauses[i].Clause);
            }
        }

        sb.Append(";");

        // Return SQL string and parameters dictionary
        return (sb.ToString(), new Dictionary<string, object>(_parameters));
    }
}