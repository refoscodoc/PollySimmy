using MediatR;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using PollySimmy.Queries;

namespace PollySimmy.Handlers.Horse;

public class GetAllHorsesHandler: IRequestHandler<GetAllHorsesQuery, List<Models.Horse>>
{
    private readonly DataContext _context;

    public GetAllHorsesHandler(DataContext context)
    {
        _context = context;
        
    }
    public async Task<List<Models.Horse>> Handle(GetAllHorsesQuery request, CancellationToken cancellationToken)
    {
        return await _context.HorseTable.Select(x => x).ToListAsync(cancellationToken);
    }
}