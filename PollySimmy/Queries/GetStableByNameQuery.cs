using MediatR;
using PollySimmy.Models;

namespace PollySimmy.Queries;

public class GetStableByNameQuery : IRequest<Stable>
{
    public readonly string Name;

    public GetStableByNameQuery(string name)
    {
        Name = name;
    }
}