namespace HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests.Models;

public record Cart
{
    public long Id { get; init; }
    public Customers CustomerId { get; init; } = null!;
    public string Item { get; init; } = null!;
}
