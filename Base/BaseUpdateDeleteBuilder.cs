using System;
using System.Collections.Generic;
using static HeadLess.SQLBuilder.Utils.Helpers;

namespace HeadLess.SQLBuilder.Base;
public abstract class BaseUpdateDeleteBuilder<TBuilder, T> : BaseSqlBuilder<TBuilder, T>
    where TBuilder : BaseUpdateDeleteBuilder<TBuilder, T>
    where T : class
{
    protected readonly List<string> _joins = new();

    protected TBuilder AddJoin(string joinType, Type otherType, string fromAlias, string fromColumn, string toAlias, string toColumn)
    {
        var otherAlias = GetAliasFromTypeName(otherType);
        _joins.Add($"{joinType} {otherType.Name} AS {otherAlias} ON {fromAlias}.{fromColumn} = {toAlias}.{toColumn}");
        return (TBuilder)this;
    }

    public TBuilder Join<TOther>(string fromColumn, string toColumn) where TOther : class
    {
        var fromAlias = GetAlias(typeof(T));
        return AddJoin("JOIN", typeof(TOther), fromAlias, fromColumn, GetAliasFromTypeName(typeof(TOther)), toColumn);
    }

    public TBuilder LeftJoin<TOther>(string fromColumn, string toColumn) where TOther : class
    {
        var fromAlias = GetAlias(typeof(T));
        return AddJoin("LEFT JOIN", typeof(TOther), fromAlias, fromColumn, GetAliasFromTypeName(typeof(TOther)), toColumn);
    }

    public TBuilder RightJoin<TOther>(string fromColumn, string toColumn) where TOther : class
    {
        var fromAlias = GetAlias(typeof(T));
        return AddJoin("RIGHT JOIN", typeof(TOther), fromAlias, fromColumn, GetAliasFromTypeName(typeof(TOther)), toColumn);
    }

    // Joins with an additional alias type
    public TBuilder Join<TOther, TAdditional>(string fromColumn, string toColumn)
        where TOther : class
        where TAdditional : class
    {
        return AddJoin("JOIN", typeof(TOther),
            GetAliasFromTypeName(typeof(TAdditional)), fromColumn,
            GetAliasFromTypeName(typeof(TOther)), toColumn);
    }

    public TBuilder LeftJoin<TOther, TAdditional>(string fromColumn, string toColumn)
        where TOther : class
        where TAdditional : class
    {
        return AddJoin("LEFT JOIN", typeof(TOther),
            GetAliasFromTypeName(typeof(TAdditional)), fromColumn,
            GetAliasFromTypeName(typeof(TOther)), toColumn);
    }

    public TBuilder RightJoin<TOther, TAdditional>(string fromColumn, string toColumn)
        where TOther : class
        where TAdditional : class
    {
        return AddJoin("RIGHT JOIN", typeof(TOther),
            GetAliasFromTypeName(typeof(TAdditional)), fromColumn,
            GetAliasFromTypeName(typeof(TOther)), toColumn);
    }
}
