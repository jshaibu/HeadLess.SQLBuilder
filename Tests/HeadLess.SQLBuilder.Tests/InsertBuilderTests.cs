using System.Collections;
using HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests.Models;
using HeadLess.SQLBuilder.Builders;

namespace HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests;

// -------------------- INSERTBUILDER TESTS --------------------
public class InsertBuilderTests
{
    [Fact]
    public void InsertBuilder_GeneratesCorrectInsertSql()
    {
        var user = new User { Id = 1, Name = "John", Email = "john@example.com" };
        var (sql, parameters) = new InsertBuilder<User>(user).AutoMap().Build();

        Assert.Contains("INSERT INTO User", sql);
        Assert.Contains("Name", sql);
        Assert.Contains("Email", sql);
        Assert.NotEmpty((IEnumerable)parameters);
    }
}
