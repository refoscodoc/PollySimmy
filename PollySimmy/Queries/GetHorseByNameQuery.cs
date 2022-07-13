using MediatR;
using PollySimmy.Models;

namespace PollySimmy.Queries;

public class GetHorseByNameQuery : IRequest<Horse>
{
    public readonly string Name;

    public GetHorseByNameQuery(string name)
    {
        Name = name;
    }
}