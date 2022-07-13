using MediatR;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using PollySimmy.Queries;

namespace PollySimmy.Handlers.Horse;

public class GetHorseByIdHandler : IRequestHandler<GetHorseByIdQuery, Models.Horse>
{
    private readonly DataContext _context;

    public GetHorseByIdHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Models.Horse> Handle(GetHorseByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.HorseTable.FirstOrDefaultAsync(x => x.Id == request.HorseId);
        return user;
    }
}