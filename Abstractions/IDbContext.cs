namespace HeadLess.SQLBuilder.Abstractions;

public interface IDbContext
{
    System.Data.IDbConnection GetDefaultConnection();
    System.Data.IDbConnection GetReadConnection();
    System.Data.IDbConnection GetWriteConnection();
}
