using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using static System.Console;
using HeadLess.SQLBuilder.Base;
using HeadLess.SQLBuilder.Models;

namespace HeadLess.SQLBuilder.Builders;

public class SqlBuilder<T> where T : class
{
    private List<string> _selects = new();
    private readonly List<string> _joins = new();
    private readonly List<(string Logic, string Clause)> _wheres = new();
    private readonly List<string> _orderBys = new();
    private readonly List<string> _groupBys = new();
    private readonly List<string> _havings = new();

    private readonly Dictionary<Type, string> _aliases = new();
    private readonly Dictionary<string, object> _parameters = new();
    
    private List<SqlBuilder<T>> _unionBuilders = new();

    private int _limit = 0;
    private int? _offset = null;
    internal int _paramIndex = 0;  // made internal to share between builders
    private bool _isUnionAll = false;

    private string Alias => GetAliasFromTypeName(typeof(T));
    private string TableName => typeof(T).Name;
    
    private bool _isDistinct = false;

    // Constructor that allows sharing paramIndex
    public SqlBuilder(int paramIndex = 0)
    {
        _paramIndex = paramIndex;
    }

    public SqlBuilder() : this(0) { }

    public SqlBuilder<T> Distinct()
    {
        _isDistinct = true;
        return this;
    }
    
    public SqlBuilder<T> SelectAll()
    {
        var columns = typeof(T).GetProperties()
            .Select(p => $"{Alias}.{p.Name}");
        _selects.AddRange(columns);
        _aliases[typeof(T)] = Alias;
        return this;
    }

    public SqlBuilder<T> SelectAll<TDto>() where TDto : class
    {
        var alias = GetAlias(typeof(TDto));
        var columns = typeof(TDto).GetProperties()
            .Select(p => $"{alias}.{p.Name}");
        _selects.AddRange(columns);
        return this;
    }

    public SqlBuilder<T> Select(params string[] columns)
    {
        _selects.AddRange(columns.Select(c => $"{Alias}.{c}"));
        return this;
    }
    
    public SqlBuilder<T> Count(string? column = $"*")
    {
        _selects.Clear();
        _selects.Add($"COUNT({ column }) AS TotalElements");
        return this;
    }    

    public SqlBuilder<T> SelectRaw(string columns)
    {
        _selects.Clear();
        _selects.Add(columns);
        return this;
    }
    
    public SqlBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        return WhereLambdaType(predicate, alias: Alias);
    }

    public SqlBuilder<T> OrWhere(Expression<Func<T, bool>> predicate)
    {
        return WhereLambdaType(predicate, alias: Alias, "OR");
    }

    public SqlBuilder<T> Where<TWhereOther>(
        Expression<Func<TWhereOther, bool>> predicate) where TWhereOther : class
    {
        var columnName = typeof(TWhereOther).Name;
        return WhereLambdaType(predicate, alias: columnName);
    }

    public SqlBuilder<T> OrWhere<TWhereOther>(
        Expression<Func<TWhereOther, bool>> predicate) where TWhereOther : class
    {
        var columnName = typeof(TWhereOther).Name;
        return WhereLambdaType(predicate, alias: columnName, chain: "OR");
    }

    private SqlBuilder<T> WhereLambdaType<TWhereOther>(
        Expression<Func<TWhereOther, bool>> predicate,
        string alias,
        string chain = "AND") where TWhereOther : class
    {
        string column;
        string op;
        object value;

        switch (predicate.Body)
        {
            case BinaryExpression binary:
                column = ExtractColumnName(binary.Left);
                op = GetSqlOperator(binary.NodeType);
                value = ExtractValue(binary.Right);
                break;

            case MethodCallExpression methodCall when methodCall.Method.Name == "Contains":
                column = ExtractColumnName(methodCall.Object ?? methodCall.Arguments[1]);
                op = "LIKE";
                value = $"%{ExtractValue(methodCall.Arguments[0])}%";
                break;

            default:
                throw new NotSupportedException($"Unsupported expression type: {predicate.Body.NodeType}");
        }

        return AddWhere(chain, alias, column, op, value);
    }
    
    private string ExtractColumnName(Expression expr)
    {
        if (expr is MemberExpression member)
            return member.Member.Name;

        if (expr is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            return unaryMember.Member.Name;

        throw new InvalidOperationException("Could not extract column name from expression.");
    }

    private object ExtractValue(Expression expr)
    {
        switch (expr)
        {
            case ConstantExpression constant:
                return constant.Value;

            case MemberExpression member when member.Expression is ConstantExpression ce:
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

    private string GetSqlOperator(ExpressionType nodeType)
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
    
    public SqlBuilder<T> Where(string column, string op, object value)
        => AddWhere("AND", Alias, column, op, value);

    public SqlBuilder<T> OrWhere(string column, string op, object value)
        => AddWhere("OR", Alias, column, op, value);

    public SqlBuilder<T> Where(string alias, string column, string op, object value)
        => AddWhere("AND", alias, column, op, value);

    public SqlBuilder<T> OrWhere(string alias, string column, string op, object value)
        => AddWhere("OR", alias, column, op, value);

    private SqlBuilder<T> AddWhere(string logic, string alias, string column, string op, object value, bool wrapGroup = false)
    {
        var paramName = $"@p{_paramIndex++}";
        var clause = $"{alias}.{column} {op} {paramName}";

        if (wrapGroup)
            clause = $"({clause})";

        _wheres.Add((logic, clause));
        _parameters[paramName] = value;
        return this;
    }

    // New method: support WhereGroup for nested AND grouping
    public SqlBuilder<T> WhereGroup(Func<SqlBuilder<T>, SqlBuilder<T>> groupBuilder)
    {
        // Create a temporary builder sharing current _paramIndex to avoid param name conflicts
        var tempBuilder = new SqlBuilder<T>(_paramIndex);
        groupBuilder(tempBuilder);

        var groupClauses = tempBuilder._wheres;
        if (groupClauses.Count == 0) return this;

        // Build grouped clause string with parentheses
        var groupedSql = "(" + groupClauses[0].Clause;
        foreach (var clause in groupClauses.Skip(1))
        {
            groupedSql += $" {clause.Logic} {clause.Clause}";
        }
        groupedSql += ")";

        // Add grouped clause with AND logic by default
        _wheres.Add(("AND", groupedSql));

        // Merge parameters from the temp builder
        foreach (var kvp in tempBuilder._parameters)
            _parameters[kvp.Key] = kvp.Value;

        // Update paramIndex so outer builder knows the correct next param number
        _paramIndex = tempBuilder._paramIndex;

        return this;
    }

    public SqlBuilder<T> GroupBy(params string[] columns)
    {
        _groupBys.AddRange(columns.Select(c => $"{Alias}.{c}"));
        return this;
    }

    public SqlBuilder<T> Having(string column, string op, object value)
    {
        var paramName = $"@p{_paramIndex++}";
        _havings.Add($"{Alias}.{column} {op} {paramName}");
        _parameters[paramName] = value;
        return this;
    }

    public SqlBuilder<T> OrderBy(string column, bool descending = false)
    {
        _orderBys.Clear();
        _orderBys.Add($"{Alias}.{column}" + (descending ? " DESC" : " ASC"));
        return this;
    }

    // 1. Order by a column on the main model T 
    public SqlBuilder<T> OrderBy(Expression<Func<T, dynamic>> selector, OrderDirection direction = OrderDirection.ASC)
    {
        var column = GetPropertyName(selector);
        _orderBys.Clear();
        _orderBys.Add($"{column} {direction.ToString().ToUpper()}");
        return this;
    }

    // 2. Order by a column from a joined table, with alias provided
    public SqlBuilder<T> OrderBy<TJoin>(Expression<Func<TJoin, dynamic>> aliasExpr, Expression<Func<TJoin, dynamic>> selector, OrderDirection direction = OrderDirection.ASC)
    {
        var alias = GetPropertyName(aliasExpr);
        var column = GetPropertyName(selector);
        _orderBys.Clear();
        _orderBys.Add($"{alias}.{column} {direction.ToString().ToUpper()}");
        return this;
    }
    
    private string GetPropertyName<TProp>(Expression<Func<TProp>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException("Invalid expression");
    }

    private string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression innerMember)
            return innerMember.Member.Name;
        throw new ArgumentException("Invalid expression");
    }
    
    public SqlBuilder<T> Limit(int count, int? offset = null)
    {
        _limit = count;
        _offset = offset;
        return this;
    }

    public SqlBuilder<T> Join<TOther>(string fromColumn, string toColumn)
    {
        var otherTable = typeof(TOther).Name;
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"JOIN {otherTable} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
        return this;
    }
    
    public SqlBuilder<T> LeftJoin<TOther>(string fromColumn, string toColumn)
    {
        var otherTable = typeof(TOther).Name;
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"LEFT JOIN {otherTable} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
        return this;
    } 
    
    public SqlBuilder<T> RightJoin<TOther>(string fromColumn, string toColumn)
    {
        var otherTable = typeof(TOther).Name;
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"RIGHT JOIN {otherTable} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
        return this;
    }      

    public SqlBuilder<T> Join<TOther, TAdditional>(string fromColumn, string toColumn)
    {
        var otherTable = typeof(TOther).Name;
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"JOIN {otherTable} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
        return this;
    }
    
    public SqlBuilder<T> LeftJoin<TOther, TAdditional>(string fromColumn, string toColumn)
    {
        var otherTable = typeof(TOther).Name;
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"LEFT JOIN {otherTable} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
        return this;
    }     
    
    public SqlBuilder<T> RightJoin<TOther, TAdditional>(string fromColumn, string toColumn)
    {
        var otherTable = typeof(TOther).Name;
        var otherAlias = GetAlias(typeof(TOther));
        _joins.Add($"RIGHT JOIN {otherTable} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
        return this;
    }    
    
    public SqlBuilder<T> Union(SqlBuilder<T> otherBuilder, bool all = false)
    {
        _unionBuilders.Add(otherBuilder);
        _isUnionAll = all;
        return this;
    } 
    
    public SqlBuilder<T> UnionAll(SqlBuilder<T> otherBuilder)
    {
        _unionBuilders.Add(otherBuilder);
        _isUnionAll = true;
        return this;
    }     

    private string GetAlias(Type type)
    {
        if (!_aliases.TryGetValue(type, out var alias))
        {
            alias = GetAliasFromTypeName(type);
            _aliases[type] = alias;
        }

        return alias;
    }

    private string GetAliasFromTypeName(Type type)
    {
        var name = type.Name;
        return name; 
    }

    public (string Sql, object Parameters) ToSql()
    {
        var selectClause = _isDistinct ? "SELECT DISTINCT" : "SELECT";
        
        if (_selects.Count == 0)
            SelectAll();

        var selectPart = string.Join(",\n    ", _selects);
        var sql = $@"
{ selectClause } 
    {selectPart}
FROM
    {TableName} AS {Alias}
{string.Join("\n", _joins)}";

        if (_wheres.Count > 0)
        {
            var first = _wheres.First().Clause;
            var rest = _wheres.Skip(1).Select(w => $"{w.Logic} {w.Clause}");
            sql += $"\nWHERE {first}";
            if (rest.Any())
                sql += $"\n    {string.Join("\n    ", rest)}";
        }

        if (_groupBys.Count > 0)
            sql += $"\nGROUP BY {string.Join(", ", _groupBys)}";

        if (_havings.Count > 0)
            sql += $"\nHAVING {string.Join(" AND ", _havings)}";

        if (_orderBys.Count > 0)
            sql += $"\nORDER BY {string.Join(", ", _orderBys)}";

        if (_limit > 0)
        {
            sql += $"\nLIMIT {_limit}";
            if (_offset.HasValue)
                sql += $" OFFSET {_offset.Value}";
        }
        
        if (_unionBuilders.Any())
        {
            var unionKeyword = _isUnionAll ? "UNION ALL" : "UNION";
            foreach (var builder in _unionBuilders)
            {
                sql += $" {unionKeyword} {builder.ToSql()}";
            }
        }        

        return (sql.Trim(), _parameters);
    }
    
    public async Task<List<dynamic>> ToListAsync(IDbConnection connection)
    {
        var (sql, parameters) = ToSql();
        WriteLine($"{sql}\n");

        return (await connection.QueryAsync(sql, parameters)).ToList();
    }
}
