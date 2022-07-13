using MediatR;
using PollySimmy.Models;

namespace PollySimmy.Commands;

public record AddHorseCommand : IRequest<Horse>
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public int Price { get; init; }
    public int Age { get; init; }
    public string? HorseJoke { get; init; }
}