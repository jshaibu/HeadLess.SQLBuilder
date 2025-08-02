using System.Data;
using MySql.Data.MySqlClient;
using Npgsql;
using Microsoft.Data.SqlClient;
using MySql.Data;

namespace HeadLess.SQLBuilder.Utils;

public static class DbFactory
{
    public static IDbConnection Create(string connStringWithPrefix)
    {
        var parts = connStringWithPrefix.Split(':', 2);
        if (parts.Length != 2)
            throw new ArgumentException("Invalid connection string format. Use e.g. mysql:Host=...");

        var provider = parts[0].ToLower();
        var connStr = parts[1];

        return provider switch
        {
            "mysql" => new MySqlConnection(connStr),
            "pgsql" => new NpgsqlConnection(connStr),
            "mssql" => new SqlConnection(connStr),
            _ => throw new NotSupportedException($"Unsupported provider: {provider}")
        };
    }
}
