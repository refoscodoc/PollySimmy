using MediatR;
using PollySimmy.Models;

namespace PollySimmy.Queries;

public class GetStableByIdQuery : IRequest<Stable>
{
    public readonly Guid StableId;

    public GetStableByIdQuery(Guid stableId)
    {
        StableId = stableId;
    }
}