using MediatR;
using PollySimmy.Models;

namespace PollySimmy.Queries;

public class GetAllStablesQuery : IRequest<List<Stable>>
{
    
}