namespace HeadLess.SQLBuilder.Utils;

public static class Helpers
{
    public static string Escape(string input) => input.Replace("'", "''");
    
    public static object? GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }        
}


