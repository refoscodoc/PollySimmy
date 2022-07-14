using MediatR;
using PollySimmy.Commands;
using PollySimmy.DataAccess;

namespace PollySimmy.Handlers.Stable;

public class AddStableHandler : IRequestHandler<AddStableCommand, Models.Stable>
{
    private readonly DataContext _context;

    public AddStableHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Models.Stable> Handle(AddStableCommand request, CancellationToken cancellationToken)
    {
        Models.Stable newStable = new Models.Stable()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Phone = request.Phone,
            Address = request.Address,
            Motto = request.Motto
        };
        
        await _context.StableTable.AddAsync(newStable);
        await _context.SaveChangesAsync();
        return newStable;
    }
}