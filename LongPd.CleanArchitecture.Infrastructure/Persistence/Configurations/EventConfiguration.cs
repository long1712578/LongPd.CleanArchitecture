using LongPd.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Event entity.
/// ALL mapping config lives here — NO data annotations on the entity.
/// </summary>
public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever(); // Client-generated Guid

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Venue)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.TotalCapacity)
            .IsRequired();

        builder.Property(e => e.StartDate)
            .IsRequired();

        builder.Property(e => e.EndDate)
            .IsRequired();

        builder.Property(e => e.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.UpdatedBy).HasMaxLength(256);

        // Soft delete
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy).HasMaxLength(256);

        // Global query filter — automatically excludes deleted events
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Navigation: one Event has many Ticket tiers
        builder.HasMany(e => e.Tickets)
            .WithOne(t => t.Event)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for common queries
        builder.HasIndex(e => e.StartDate);
        builder.HasIndex(e => e.IsPublished);
    }
}
