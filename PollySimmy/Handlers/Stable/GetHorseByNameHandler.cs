using MediatR;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using PollySimmy.Queries;

namespace PollySimmy.Handlers.Stable;

public class GetStableByNameHandler : IRequestHandler<GetStableByNameQuery, Models.Stable>
{
    private readonly DataContext _context;

    public GetStableByNameHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Models.Stable> Handle(GetStableByNameQuery request, CancellationToken cancellationToken)
    {
        var stable = await _context.StableTable.FirstOrDefaultAsync(x => x.Name == request.Name, cancellationToken);
        return stable;
    }
}