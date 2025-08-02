using System.Text;
using HeadLess.SQLBuilder.Base;

namespace HeadLess.SQLBuilder.Builders;

public class DeleteBuilder<T> : BaseSqlBuilder<T> where T : class
{
    private string TableName => typeof(T).Name;

    protected override BaseSqlBuilder<T> CreateNewBuilder() => new DeleteBuilder<T>();

    // Override base Where methods to return DeleteBuilder<T> for fluent chaining
    public new DeleteBuilder<T> Where(string column, string op, object? value)
    {
        base.Where(column, op, value);
        return this;
    }

    public new DeleteBuilder<T> OrWhere(string column, string op, object? value)
    {
        base.OrWhere(column, op, value);
        return this;
    }

    public new DeleteBuilder<T> WhereGroup(Func<BaseSqlBuilder<T>, BaseSqlBuilder<T>> groupBuilder)
    {
        base.WhereGroup(groupBuilder);
        return this;
    }

    public (string Sql, Dictionary<string, object?> Parameters) Build()
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

        // Return SQL string and a copy of parameters dictionary
        return (sb.ToString(), new Dictionary<string, object?>(_parameters));
    }
}
