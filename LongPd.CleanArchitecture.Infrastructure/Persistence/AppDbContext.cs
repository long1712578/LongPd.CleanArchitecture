using LongPd.CleanArchitecture.Domain.Common;
using LongPd.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext — write-side database context.
/// Responsibilities:
///   1. Apply all entity configurations from this assembly (IEntityTypeConfiguration).
///   2. Apply global query filters for soft-delete entities.
///   3. SaveChangesAsync is called ONLY via IUnitOfWork (never inject DbContext directly in Application).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveColumnType("timestamp with time zone");

        configurationBuilder
            .Properties<DateTime?>()
            .HaveColumnType("timestamp with time zone");
    }
}
