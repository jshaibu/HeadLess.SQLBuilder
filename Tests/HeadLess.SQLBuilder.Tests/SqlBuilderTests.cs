using System.Collections;
using HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests.Models;
using HeadLess.SQLBuilder.Builders;

namespace HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests;

// -------------------- SQLBUILDER TESTS -----------------------
public class SqlBuilderTests
{
    [Fact]
    public void SelectAll_BasicQuery_GeneratesCorrectSql()
    {
        var query = new SqlBuilder<Users>();
        var (sql, parameters) = query.ToSql();

        Assert.Contains("SELECT", sql);
        Assert.Contains("FROM", sql);
        Assert.Contains("Users AS Users", sql);
        Assert.Empty((IEnumerable)(parameters));
    }

    [Fact]
    public void Count_WithJoins_GeneratesCorrectCountSql()
    {
        var query = new SqlBuilder<Users>()
            .Join<Customers>("Id", "UserId")
            .Join<Cart, Customers>("Id", "CustomerId");

        var (sql, parameters) = query.ToCountSql();

        Assert.Contains("SELECT", sql);
        Assert.Contains("COUNT", sql);
        Assert.Contains("FROM", sql);
        Assert.Contains("JOIN Customers AS Customers ON Users.Id = Customers.UserId", sql);
        Assert.Contains("JOIN Cart AS Cart ON Customers.Id = Cart.CustomerId", sql);
        Assert.Empty((IEnumerable)(parameters));
    }

    [Fact]
    public void InnerJoinWithWhereAndOrderBy_GeneratesCorrectSql()
    {
        var query = new SqlBuilder<Users>()
            .Join<Customers>("Id", "UserId")
            .Join<Cart, Customers>("Id", "CustomerId")
            .SelectAll<Users>()
            .SelectAll<Customers>()
            .Where<Customers>(c => c.Id == 1)
            .Where(u => u.Email.Contains("john.doe@gmail.com"))
            .WhereGroup(g => g
                .Where(c => c.Name == "John")
                .OrWhere(c => c.Email == "john.doe@gmail.com"))
            .OrderBy("Name");

        var (sql, parameters) = query.ToSql();
        
        Assert.Contains("SELECT", sql);
        Assert.Contains("Users.Id", sql);
        Assert.Contains("FROM", sql);
        Assert.Contains("Users AS Users", sql);
        Assert.Contains("JOIN Customers AS Customers ON Users.Id = Customers.UserId", sql);
        Assert.Contains("JOIN Cart AS Cart ON Customers.Id = Cart.CustomerId", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("ORDER BY Users.Name ASC", sql);
        Assert.NotEmpty((IEnumerable)parameters);
    }

    [Fact]
    public void RawWhereQuery_GeneratesCorrectSql()
    {
        var query = new SqlBuilder<Users>()
            .Join<Customers>("Id", "UserId")
            .Where("Customers", "Phone", "=", "0881327151");

        var (sql, parameters) = query.ToSql();

        Assert.Contains("WHERE Customers.Phone = @p0", sql);
        
        var paramDict = parameters as IDictionary<string, object>;
        Assert.NotNull(paramDict);            // Ensure cast worked
        Assert.Single(paramDict);             // Ensure only one parameter
        Assert.Equal("0881327151", paramDict["@p0"]);
    }

    [Fact]
    public void LimitQuery_GeneratesCorrectSql()
    {
        var query = new SqlBuilder<Users>()
            .Join<Customers>("Id", "UserId")
            .Limit(10, 5);

        var (sql, parameters) = query.ToSql();

        Assert.Contains("LIMIT 10", sql);
        Assert.Contains("OFFSET 5", sql);
    }

    [Fact]
    public void LeftJoinQuery_GeneratesCorrectSql()
    {
        var query = new SqlBuilder<Users>()
            .LeftJoin<Customers>("Id", "UserId");

        var (sql, parameters) = query.ToSql();
        Assert.Contains("LEFT JOIN Customers AS Customers ON Users.Id = Customers.UserId", sql);
    }

    [Fact]
    public void RightJoinQuery_GeneratesCorrectSql()
    {
        var query = new SqlBuilder<Users>()
            .RightJoin<Customers>("Id", "UserId");

        var (sql, parameters) = query.ToSql();

        Assert.Contains("RIGHT JOIN Customers AS Customers ON Users.Id = Customers.UserId", sql);
    }
}