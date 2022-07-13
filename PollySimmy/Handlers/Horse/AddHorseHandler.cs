using MediatR;
using PollySimmy.Commands;
using PollySimmy.DataAccess;

namespace PollySimmy.Handlers.Horse;

public class AddHorseHandler : IRequestHandler<AddHorseCommand, Models.Horse>
{
    private readonly DataContext _context;

    public AddHorseHandler(DataContext context)
    {
        _context = context;
    }

    public async Task<Models.Horse> Handle(AddHorseCommand request, CancellationToken cancellationToken)
    {
        Models.Horse newHorse = new Models.Horse
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Age = 0,
            Price = 100
        };
        
        await _context.HorseTable.AddAsync(newHorse);
        await _context.SaveChangesAsync();
        return newHorse;
    }
}