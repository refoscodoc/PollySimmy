using MediatR;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using PollySimmy.Queries;

namespace PollySimmy.Handlers.Horse;

public class GetHorseByNameHandler : IRequestHandler<GetHorseByNameQuery, Models.Horse>
{
    private readonly DataContext _context;

    public GetHorseByNameHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Models.Horse> Handle(GetHorseByNameQuery request, CancellationToken cancellationToken)
    {
        var horse = await _context.HorseTable.FirstOrDefaultAsync(x => x.Name == request.Name, cancellationToken);
        return horse;
    }
}