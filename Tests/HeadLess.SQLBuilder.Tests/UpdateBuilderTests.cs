using HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests.Models;
using HeadLess.SQLBuilder.Builders;
using Xunit.Abstractions;

namespace HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests;

// -------------------- UPDATEBUILDER TESTS --------------------
public class UpdateBuilderTests
{
    [Fact]
    public void UpdateBuilder_GeneratesCorrectUpdateSql()
    { 
        var user = new User { Name = "Jane", Email = "jane@example.com" };
        var (sql, parameters) = new UpdateBuilder<User>(user)
            .AutoMap()
            .Where("Id", "=", 1)
            .Build();
        
        Assert.Contains("UPDATE User", sql);
        Assert.Contains("WHERE Id = @p2", sql);
        Assert.NotEmpty(parameters);
    }
    
    [Fact]
    public void StronglyTypedUpdateBuilder_GeneratesCorrectSql()
    {
        var user = new User { Name = "Jane", Email = "jane@example.com" };
        var query = new UpdateBuilder<User>(user).AutoMap();
        query.Join<Customers>("Id", "UserId");
        query.Set<User>(c => c.Email, "john.doe@gmail.com");
        query.Where(c => c.Id == 1);
        query.WhereGroup(q => q
            .Where(c => c.Email == "john.doe@gmail.com")
            .OrWhere(c => c.Name == "John Doe")
            .Where<Customers>(c => c.Phone == "0881327151"));

        var (sql, parameters) = query.Build();
        
        Assert.Contains("UPDATE User AS User", sql);
        Assert.Contains("JOIN Customers AS Customers ON User.Id = Customers.UserId", sql);
        Assert.Contains("SET User.Name = @p0", sql);
        Assert.Contains("WHERE", sql);
        Assert.NotEmpty(parameters);
    }
}
