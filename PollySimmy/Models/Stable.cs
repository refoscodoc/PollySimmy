namespace PollySimmy.Models;

public record Stable
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Address { get; init; }
    public int Phone { get; init; }
    public string? Motto { get; init; }
}