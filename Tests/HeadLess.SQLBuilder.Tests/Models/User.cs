namespace HeadLess.SQLBuilder.Tests.HeadLess.SQLBuilder.Tests.Models;

public record User
{
    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public string Email { get; init; } = null!;
}
