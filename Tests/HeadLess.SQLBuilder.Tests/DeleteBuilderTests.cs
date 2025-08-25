using HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests.Models;
using HeadLess.SQLBuilder.Builders;

namespace HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests;

// -------------------- DELETEBUILDER TESTS --------------------
public class DeleteBuilderTests
{
    [Fact]
    public void DeleteBuilder_GeneratesCorrectDeleteSql()
    {
        var (sql, parameters) = new DeleteBuilder<Customers>()
            .Where("Id", "=", 123)
            .Build();

        Assert.Contains("DELETE FROM Customers", sql);
        Assert.Contains("WHERE Id = @p0;", sql);
        Assert.Single(parameters);
        Assert.Equal(123, parameters["@p0"]);
    }

    [Fact]
    public void StronglyTypedDeleteBuilder_GeneratesCorrectSql()
    {
        var query = new DeleteBuilder<Customers>();
        query.Join<User>("Id", "UserId");
        query.Where(c => c.Id == 123);
        query.WhereGroup(q => q
            .Where(c => c.Phone == "0881327151")
            .OrWhere(c => c.Phone == "0998374657"));

        var (sql, parameters) = query.Build();

        Assert.Contains("DELETE FROM Customers AS Customers", sql);
        Assert.Contains("JOIN User AS User ON Customers.Id = User.UserId", sql);
        Assert.Contains("WHERE", sql);
        Assert.NotEmpty(parameters);
    }
}
