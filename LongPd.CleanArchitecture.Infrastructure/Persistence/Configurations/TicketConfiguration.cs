using LongPd.CleanArchitecture.Domain.Entities;
using LongPd.CleanArchitecture.Domain.Enums;
using LongPd.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Ticket entity.
/// Key mapping: Money value object is stored as owned entity (two columns: PriceAmount, PriceCurrency).
/// RowVersion mapped to PostgreSQL's xmin system column for optimistic concurrency.
/// </summary>
public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.TierName)
            .IsRequired()
            .HasMaxLength(100);

        // Map Money value object as owned entity (flattened columns)
        builder.OwnsOne(t => t.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(t => t.TotalQuantity).IsRequired();
        builder.Property(t => t.AvailableQuantity).IsRequired();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasDefaultValue(TicketStatus.Available)
            .HasConversion<int>(); // Store as integer for performance

        // ─── Optimistic Concurrency (PostgreSQL xmin) ─────────────────────────
        // PostgreSQL automatically increments xmin on every row update.
        // EF Core uses this as a concurrency token — no extra column needed.
        builder.Property(t => t.RowVersion).IsRowVersion();

        // Audit fields
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt);
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.UpdatedBy).HasMaxLength(256);

        // Soft delete
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt);
        builder.Property(t => t.DeletedBy).HasMaxLength(256);

        // Global query filter
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Indexes for hot queries
        builder.HasIndex(t => t.EventId);
        builder.HasIndex(t => new { t.EventId, t.Status });
    }
}
