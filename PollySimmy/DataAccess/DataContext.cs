using Microsoft.EntityFrameworkCore;
using PollySimmy.Models;

namespace PollySimmy.DataAccess;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) {}
    
    public DbSet<Horse> HorseTable { get; set; }
    public DbSet<Stable> StableTable { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Horse>().HasKey(m => m.Id);
        builder.Entity<Stable>().HasKey(m => m.Id);

        base.OnModelCreating(builder);
    }
}
