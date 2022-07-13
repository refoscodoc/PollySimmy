using MediatR;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using PollySimmy.Queries;

namespace PollySimmy.Handlers.Stable;

public class GetStableByIdHandler : IRequestHandler<GetStableByIdQuery, Models.Stable>
{
    private readonly DataContext _context;

    public GetStableByIdHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Models.Stable> Handle(GetStableByIdQuery request, CancellationToken cancellationToken)
    {
        var stable = await _context.StableTable.FirstOrDefaultAsync(x => x.Id == request.StableId);
        return stable;
    }
}