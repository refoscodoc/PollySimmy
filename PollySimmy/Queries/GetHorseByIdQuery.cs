using MediatR;
using PollySimmy.Models;

namespace PollySimmy.Queries;

public class GetHorseByIdQuery : IRequest<Horse>
{
    public readonly Guid HorseId;

    public GetHorseByIdQuery(Guid horseId)
    {
        HorseId = horseId;
    }
}