using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using HeadLess.SQLBuilder.Base;
using HeadLess.SQLBuilder.Models;
using static HeadLess.SQLBuilder.Utils.Helpers;

namespace HeadLess.SQLBuilder.Builders
{
    public class SqlBuilder<T> : WhereClauseBuilderBase<SqlBuilder<T>, T>
        where T : class
    {
        private readonly List<string> _selects = new();
        private readonly List<string> _joins = new();
        private readonly List<string> _orderBys = new();
        private readonly List<string> _groupBys = new();
        private readonly List<string> _havings = new();
        private readonly Dictionary<Type, string> _aliases = new();
        private readonly List<SqlBuilder<T>> _unionBuilders = new();

        private int _limit = 0;
        private int? _offset = null;
        private bool _isDistinct = false;
        private bool _isUnionAll = false;

        private string Alias => GetAlias(typeof(T));
        private string TableName => typeof(T).Name;

        public SqlBuilder() { }
        public SqlBuilder(int paramIndex) => _paramIndex = paramIndex;

        protected override string GetAlias(Type type)
        {
            if (!_aliases.TryGetValue(type, out var alias))
            {
                alias = GetAliasFromTypeName(type);
                _aliases[type] = alias;
            }
            return alias;
        }

        public SqlBuilder<T> Distinct()
        {
            _isDistinct = true;
            return this;
        }

        public SqlBuilder<T> SelectAll()
        {
            var columns = typeof(T).GetProperties().Select(p => $"{Alias}.{p.Name}");
            _selects.AddRange(columns);
            _aliases[typeof(T)] = Alias;
            return this;
        }

        public SqlBuilder<T> SelectAll<TDto>() where TDto : class
        {
            var alias = GetAlias(typeof(TDto));
            var columns = typeof(TDto).GetProperties().Select(p => $"{alias}.{p.Name}");
            _selects.AddRange(columns);
            return this;
        }

        public SqlBuilder<T> Select(params string[] columns)
        {
            _selects.AddRange(columns.Select(c => $"{Alias}.{c}"));
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

        public SqlBuilder<T> OrderBy(Expression<Func<T, dynamic>> selector, OrderDirection direction = OrderDirection.ASC)
        {
            var column = GetPropertyName(selector);
            _orderBys.Clear();
            _orderBys.Add($"{column} {direction}");
            return this;
        }

        public SqlBuilder<T> OrderBy<TJoin>(Expression<Func<TJoin, dynamic>> aliasExpr,
                                            Expression<Func<TJoin, dynamic>> selector,
                                            OrderDirection direction = OrderDirection.ASC)
        {
            var alias = GetPropertyName(aliasExpr);
            var column = GetPropertyName(selector);
            _orderBys.Clear();
            _orderBys.Add($"{alias}.{column} {direction}");
            return this;
        }

        public SqlBuilder<T> Limit(int count, int? offset = null)
        {
            _limit = count;
            _offset = offset;
            return this;
        }

        public SqlBuilder<T> Join<TOther>(string fromColumn, string toColumn)
        {
            var otherAlias = GetAlias(typeof(TOther));
            _joins.Add($"JOIN {typeof(TOther).Name} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
            return this;
        }

        public SqlBuilder<T> LeftJoin<TOther>(string fromColumn, string toColumn)
        {
            var otherAlias = GetAlias(typeof(TOther));
            _joins.Add($"LEFT JOIN {typeof(TOther).Name} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
            return this;
        }

        public SqlBuilder<T> RightJoin<TOther>(string fromColumn, string toColumn)
        {
            var otherAlias = GetAlias(typeof(TOther));
            _joins.Add($"RIGHT JOIN {typeof(TOther).Name} AS {otherAlias} ON {Alias}.{fromColumn} = {otherAlias}.{toColumn}");
            return this;
        }

        public SqlBuilder<T> Join<TOther, TAdditional>(string fromColumn, string toColumn)
        {
            var otherAlias = GetAlias(typeof(TOther));
            _joins.Add($"JOIN {typeof(TOther).Name} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
            return this;
        }

        public SqlBuilder<T> LeftJoin<TOther, TAdditional>(string fromColumn, string toColumn)
        {
            var otherAlias = GetAlias(typeof(TOther));
            _joins.Add($"LEFT JOIN {typeof(TOther).Name} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
            return this;
        }

        public SqlBuilder<T> RightJoin<TOther, TAdditional>(string fromColumn, string toColumn)
        {
            var otherAlias = GetAlias(typeof(TOther));
            _joins.Add($"RIGHT JOIN {typeof(TOther).Name} AS {otherAlias} ON {typeof(TAdditional).Name}.{fromColumn} = {otherAlias}.{toColumn}");
            return this;
        }

        public (string Sql, object Parameters) ToCountSql()
        {
            var (sql, parameters) = ToSql();

            var firstPropInfo = typeof(T).GetProperties().FirstOrDefault();
            string alias = GetAliasFromTypeName(typeof(T));  // e.g. "u"
    
            var columnName = firstPropInfo != null 
                ? $"{alias}.{GetColumnName(firstPropInfo)}" 
                : "*";

            // Replace SELECT ... FROM with SELECT COUNT(column) FROM
            var selectPattern = @"SELECT\s+(.+?)\s+FROM";
            sql = System.Text.RegularExpressions.Regex.Replace(sql, selectPattern,
                $"SELECT COUNT({columnName}) FROM", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            // Remove ORDER BY clause
            var orderByPattern = @"ORDER BY[\s\S]*?(?=(LIMIT|OFFSET|$))";
            sql = System.Text.RegularExpressions.Regex.Replace(sql, orderByPattern, "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return (sql.Trim(), parameters);
        }

        public async Task<int> CountAsync(IDbConnection connection)
        {
            var (sql, parameters) = ToCountSql();
            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
            return count;
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

        public (string Sql, object Parameters) ToSql()
        {
            var selectClause = _isDistinct ? "SELECT DISTINCT" : "SELECT";

            if (_selects.Count == 0)
                SelectAll();

            var sql = $@"
{selectClause}
    {string.Join(",\n    ", _selects)}
FROM
    {TableName} AS {Alias}
{string.Join("\n", _joins)}";

            if (_whereClauses.Count > 0)
            {
                var first = _whereClauses.First().Clause;
                var rest = _whereClauses.Skip(1).Select(w => $"{w.Logic} {w.Clause}");
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
                    var (unionSql, unionParams) = builder.ToSql();
                    sql += $"\n{unionKeyword}\n{unionSql}";

                    // Merge parameters
                    foreach (var kv in (IDictionary<string, object>)unionParams)
                        _parameters[kv.Key] = kv.Value;
                }
            }

            return (sql.Trim(), _parameters);
        }

        public async Task<List<dynamic>> ToListAsync(IDbConnection connection)
        {
            var (sql, parameters) = ToSql();
            return (await connection.QueryAsync(sql, parameters)).ToList();
        }
    }
}