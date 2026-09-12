using Microsoft.EntityFrameworkCore;
using PropertyViewing.Domain.Entities;

namespace PropertyViewing.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Viewing> Viewings => Set<Viewing>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
