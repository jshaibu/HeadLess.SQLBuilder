using HeadLess.SQLBuilder.Abstractions;
using System.Data;
using HeadLess.SQLBuilder.Utils;

namespace HeadLess.SQLBuilder.Context;

public class DbContext : IDbContext
{
    private readonly string _defaultConn;
    private readonly string _readConn;
    private readonly string _writeConn;

    public DbContext(string defaultConnection, string? readConnection = "", string? writeConnection = "")
    {
        _defaultConn = defaultConnection;
        _readConn = readConnection;
        _writeConn = writeConnection;
    }

    public IDbConnection GetDefaultConnection() =>
        DbFactory.Create(_defaultConn);

    public IDbConnection GetReadConnection() =>
        DbFactory.Create(_readConn);

    public IDbConnection GetWriteConnection() =>
        DbFactory.Create(_writeConn);
}