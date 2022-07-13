using MediatR;
using PollySimmy.Models;

namespace PollySimmy.Queries;

public class GetAllHorsesQuery : IRequest<List<Horse>>
{
    
}