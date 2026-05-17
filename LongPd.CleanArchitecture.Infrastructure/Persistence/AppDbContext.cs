using LongPd.CleanArchitecture.Domain.Common;
using LongPd.CleanArchitecture.Domain.Entities;
using LongPd.CleanArchitecture.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext — write-side database context.
/// Responsibilities:
/// - Apply all entity configurations from this assembly (IEntityTypeConfiguration).
/// - Apply global query filters for soft-delete entities.
/// - SaveChangesAsync is called ONLY via IUnitOfWork (never inject DbContext directly in Application).
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
        // Automatically apply UTC Converter to all DateTime properties globally.
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>()
            .HaveColumnType("timestamp with time zone");

        // EF Core will automatically apply the base value converter for nullable types too.
        configurationBuilder
            .Properties<DateTime?>()
            .HaveConversion<UtcDateTimeConverter>()
            .HaveColumnType("timestamp with time zone");
    }
}
