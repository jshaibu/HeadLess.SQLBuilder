using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HeadLess.SQLBuilder.Utils;

namespace HeadLess.SQLBuilder.Base;

public abstract class BaseSqlBuilder<TBuilder, TModel>
    where TBuilder : BaseSqlBuilder<TBuilder, TModel>
    where TModel : class
{
    protected readonly List<(string Logic, string Clause)> _whereClauses = new();
    protected readonly List<string> _joins = new();
    protected readonly Dictionary<string, object> _parameters = new();
    protected int _paramIndex = 0;

    protected string Alias => GetAlias(typeof(TModel));
    
    protected abstract string GetAlias(Type type);

    public TBuilder Where(Expression<Func<TModel, bool>> predicate)
        => (TBuilder)WhereLambdaType(predicate, GetAlias(typeof(TModel)));

    public TBuilder OrWhere(Expression<Func<TModel, bool>> predicate)
        => (TBuilder)WhereLambdaType(predicate, GetAlias(typeof(TModel)), "OR");

    public TBuilder Where<TOther>(Expression<Func<TOther, bool>> predicate) where TOther : class
        => (TBuilder)WhereLambdaType(predicate, typeof(TOther).Name);

    public TBuilder OrWhere<TOther>(Expression<Func<TOther, bool>> predicate) where TOther : class
        => (TBuilder)WhereLambdaType(predicate, typeof(TOther).Name, "OR");

    protected TBuilder WhereLambdaType<TOther>(
        Expression<Func<TOther, bool>> predicate,
        string alias,
        string chain = "AND") where TOther : class
    {
        string column;
        string op;
        object value;

        switch (predicate.Body)
        {
            case BinaryExpression binary:
                column = WhereClauseUtils.ExtractColumnName(binary.Left);
                op = WhereClauseUtils.GetSqlOperator(binary.NodeType);
                value = WhereClauseUtils.ExtractValue(binary.Right);
                break;

            case MethodCallExpression methodCall when methodCall.Method.Name == "Contains":
                column = WhereClauseUtils.ExtractColumnName(methodCall.Object ?? methodCall.Arguments[1]);
                op = "LIKE";
                value = $"%{WhereClauseUtils.ExtractValue(methodCall.Arguments[0])}%";
                break;

            default:
                throw new NotSupportedException($"Unsupported expression type: {predicate.Body.NodeType}");
        }

        return AddWhere(chain, alias, column, op, value);
    }

    public TBuilder Where(string column, string op, object value)
        => AddWhere("AND", GetAlias(typeof(TModel)), column, op, value);

    public TBuilder OrWhere(string column, string op, object value)
        => AddWhere("OR", GetAlias(typeof(TModel)), column, op, value);

    public TBuilder Where(string alias, string column, string op, object value)
        => AddWhere("AND", alias, column, op, value);

    public TBuilder OrWhere(string alias, string column, string op, object value)
        => AddWhere("OR", alias, column, op, value);

    protected TBuilder AddWhere(string logic, string alias, string column, string op, object value, bool wrapGroup = false)
    {
        var paramName = $"@p{_paramIndex++}";
        var clause = $"{alias}.{column} {op} {paramName}";

        if (wrapGroup)
            clause = $"({clause})";

        _whereClauses.Add((logic, clause));
        _parameters[paramName] = value;
        return (TBuilder)this;
    }

    public TBuilder WhereGroup(Func<TBuilder, TBuilder> groupBuilder)
    {
        var tempBuilder = (TBuilder)Activator.CreateInstance(typeof(TBuilder));
        tempBuilder._paramIndex = _paramIndex;
        groupBuilder(tempBuilder);

        if (tempBuilder._whereClauses.Count == 0) return (TBuilder)this;

        var groupedSql = "(" + tempBuilder._whereClauses[0].Clause;
        foreach (var clause in tempBuilder._whereClauses.Skip(1))
            groupedSql += $" {clause.Logic} {clause.Clause}";
        groupedSql += ")";

        _whereClauses.Add(("AND", groupedSql));

        foreach (var kvp in tempBuilder._parameters)
            _parameters[kvp.Key] = kvp.Value;

        _paramIndex = tempBuilder._paramIndex;

        return (TBuilder)this;
    }
    
    // joins
    public TBuilder Join<TOther>(string fromColumn, string toColumn)
    {
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"JOIN {typeof(TOther).Name} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
        return (TBuilder)this;
    }

    public TBuilder LeftJoin<TOther>(string fromColumn, string toColumn)
    {
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"LEFT JOIN {typeof(TOther).Name} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
        return (TBuilder)this;
    }

    public TBuilder RightJoin<TOther>(string fromColumn, string toColumn)
    {
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"RIGHT JOIN {typeof(TOther).Name} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
        return (TBuilder)this;
    }

    public TBuilder Join<TOther, TAdditional>(string fromColumn, string toColumn)
    {
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"JOIN {typeof(TOther).Name} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
        return (TBuilder)this;
    }

    public TBuilder LeftJoin<TOther, TAdditional>(string fromColumn, string toColumn)
    {
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"LEFT JOIN {typeof(TOther).Name} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
        return (TBuilder)this;
    }

    public TBuilder RightJoin<TOther, TAdditional>(string fromColumn, string toColumn)
    {
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"RIGHT JOIN {typeof(TOther).Name} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
        return (TBuilder)this;
    }    
    // end joins
}
