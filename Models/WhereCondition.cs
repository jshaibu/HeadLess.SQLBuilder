namespace HeadLess.SQLBuilder.Models;

public class WhereCondition
{
    public string Column { get; set; } = null!;
    public string Operator { get; set; } = "=";
    public object? Value { get; set; }
    public string Logic { get; set; } = "AND"; // or "OR"

    public string ToSql()
    {
        if (Value == null) return $"{Column} {Operator} NULL";

        string val = Value switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            _ => Value.ToString() ?? "NULL"
        };
        return $"{Column} {Operator} {val}";
    }
}

