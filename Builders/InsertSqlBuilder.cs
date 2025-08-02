using static HeadLess.SQLBuilder.Utils.Helpers;
using HeadLess.SQLBuilder.Base;

namespace HeadLess.SQLBuilder.Builders;
    
public class InsertBuilder<T>
{
    private readonly Dictionary<string, object?> _values = new();
    private readonly T? _model;

    public InsertBuilder(T? model = default)
    {
        if(model is not null) _model = model;
    }

    public InsertBuilder<T> Set(string column, object? value)
    {
        _values[column] = value;
        return this;
    }

    // Optional: auto-map all properties from the model
    public InsertBuilder<T> AutoMap()
    {
        if (_model is null) return this;
        
        var actualType = _model?.GetType();
        if (actualType == null) return this;

        foreach (var prop in actualType.GetProperties())
        {
            var value = prop.GetValue(_model);

            // Only include if value is not null and not default (optional)
            if (value == null || Equals(value, GetDefault(prop.PropertyType)))
                continue;

            _values[prop.Name] = value;
        }

        return this;
    }

    public (string Sql, object Parameters) Build()
    {
        string tableName = typeof(T).Name;

        var columns = string.Join(", ", _values.Keys);
        var paramNames = string.Join(", ", _values.Keys.Select(k => $"@{k}"));

        var parameters = new Dictionary<string, object?>();
        foreach (var kv in _values)
        {
            parameters[$"{kv.Key}"] = kv.Value;
        }

        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({paramNames})";
        return (sql, parameters);
    }
}
