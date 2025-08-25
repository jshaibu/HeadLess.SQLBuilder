namespace HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests.Models;

public record Customers
{
    public long Id { get; init; }
    public List<Users> UserId { get; init; } = new();
    public string Phone { get; init; } = null!;
}
