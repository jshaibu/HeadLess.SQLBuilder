# HeadLess.SQLBuilder

HeadLess.SQLBuilder is a lightweight, flexible SQL query builder designed to simplify SQL construction and database interaction using Dapper in .NET applications.

## ✨ Features

- Strongly-typed SQL query builder for Dapper.
- Supports multiple database providers: MySQL, PostgreSQL, SQL Server.
- Clean, fluent API with chainable query methods.
- Seamless dependency injection and connection context support.

## 📦 Installation

First, add references of this library and required database drivers to your project:

```bash
dotnet add reference /path-to-project/HeadLess.SQLBuilder.csproj

dotnet add package Npgsql
# PostgreSQL driver

dotnet add package MySql.Data
# MySQL driver

dotnet add package Microsoft.Data.SqlClient
# SQL Server driver

dotnet add package Dapper
# Dapper micro ORM
```

## ⚙️ Configuration

In your `Program.cs`, configure the context:

```csharp
using HeadLess.SQLBuilder.Context;
using HeadLess.SQLBuilder.Builders;
using HeadLess.SQLBuilder.Abstractions;

builder.Services.AddScoped<IDbContext>(sp => {
    // Optional: get tenant-based connection strings
    // string tenantId = sp.GetRequiredService<ITenantProvider>().GetTenantId();

    string defaultConnStr = $"mysql:{builder.Configuration.GetConnectionString("DefaultConnection")}";
    string readConnStr = $"mysql:{builder.Configuration.GetConnectionString("ReadConnection")}";
    string writeConnStr = $"mysql:{builder.Configuration.GetConnectionString("WriteConnection")}";

    return new DbContext(defaultConnStr, readConnStr, writeConnStr);
});
```

> **Note:** Prefix connection strings with the database type (`mysql:`, `pgsql:`, `mssql:`)

## 🔍 Example Usage

### Simple Select

```csharp
app.MapGet("/users", async (IDbContext _context) =>
{
    using var connection = _context.GetDefaultConnection();
    var query = new SqlBuilder<Users>();

    var (sql, parameters) = query.ToSql();
    Console.WriteLine($"Query: {sql}");

    return await query.ToListAsync(connection);
});
```

### Count with Join

```csharp
app.MapGet("/count", async (IDbContext _context) =>
{
    using var connection = _context.GetDefaultConnection();
    var query = new SqlBuilder<Users>()
        .Join<Customers>("Id", "UserId")
        .Join<Cart, Customers>("Id", "CustomerId")
        .Count();

    var (sql, parameters) = query.ToSql();
    Console.WriteLine($"Query: {sql}");

    return await query.ToListAsync(connection);
});
```

### Complex Join & Filters

```csharp
app.MapGet("/join-users-customers", async (IDbContext _context) =>
{
    using var connection = _context.GetDefaultConnection();
    var query = new SqlBuilder<Users>()
        .Join<Customers>("Id", "UserId")
        .Join<Cart, Customers>("Id", "CustomerId")
        .SelectAll<Users>()
        .SelectAll<Customers>()
        .Where<Customers>(c => c.Id == 1)
        .Where(u => u.Email.Contains("john.doe@gmail.com"))
        .WhereGroup(g => g.Where(c => c.Name == "John")
            .OrWhere(c => c.Email == "john.doe@gmail.com"))
        .OrderBy("Name");

    var (sql, parameters) = query.ToSql();
    Console.WriteLine($"Query: {sql}");

    return await query.ToListAsync(connection);
});
```

### Insert Example

```csharp
app.MapPost("/insert-user", async ([FromBody] User user, IDbContext _context) =>
{
    using var connection = _context.GetDefaultConnection();
    var (sql, parameters) = new InsertBuilder<User>(user).AutoMap().Build();

    return Results.Ok(new { sql, parameters });
});
```

### Update Example

```csharp
app.MapPut("/update-user/{id}", async ([FromBody] User user, long id, IDbContext _context) =>
{
    using var connection = _context.GetDefaultConnection();
    var (sql, parameters) = new UpdateBuilder<User>(user).AutoMap().Where("Id", "=", id).Build();

    return Results.Ok(new { sql, parameters });
});
```

### Delete Example

```csharp
app.MapGet("/delete-user/{id}", async (long id, IDbContext _context) =>
{
    using var connection = _context.GetDefaultConnection();
    var (sql, parameters) = new DeleteBuilder<Customers>().Where("Id", "=", id).Build();

    return Results.Ok(new { sql, parameters });
});
```

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to fork the repository and submit a pull request.

# Happy Coding 👋
