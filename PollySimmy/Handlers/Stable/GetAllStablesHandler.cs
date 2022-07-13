using MediatR;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using PollySimmy.Queries;

namespace PollySimmy.Handlers.Stable;

public class GetAllStablesHandler: IRequestHandler<GetAllStablesQuery, List<Models.Stable>>
{
    private readonly DataContext _context;

    public GetAllStablesHandler(DataContext context)
    {
        _context = context;
        
    }
    public async Task<List<Models.Stable>> Handle(GetAllStablesQuery request, CancellationToken cancellationToken)
    {
        return await _context.StableTable.Select(x => x).ToListAsync(cancellationToken);
    }
}