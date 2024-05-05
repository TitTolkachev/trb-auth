using Microsoft.EntityFrameworkCore;
using trb_auth.Entities;

namespace trb_auth;

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<Device> Devices { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        Database.Migrate();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>().HasKey(x => x.Id);
    }
}