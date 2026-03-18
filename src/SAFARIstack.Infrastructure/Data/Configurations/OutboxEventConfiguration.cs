using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

/// <summary>
/// OutboxEvent entity configuration.
/// </summary>
public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.PropertyId)
            .IsRequired();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.EventData)
            .IsRequired()
            .HasColumnType("jsonb"); // PostgreSQL JSON type for efficient querying

        builder.Property(e => e.AggregateType)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.AggregateId)
            .IsRequired();

        builder.Property(e => e.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.PublishedAt);

        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.IsMovedToDeadLetter)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.LastAttemptAt);

        builder.Property(e => e.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(e => new { e.IsPublished, e.IsMovedToDeadLetter, e.PropertyId })
            .HasName("ix_outbox_events_pending");

        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasName("ix_outbox_events_idempotency");

        builder.HasIndex(e => e.CreatedAt)
            .HasName("ix_outbox_events_created_at");

        builder.HasIndex(e => e.IsMovedToDeadLetter)
            .HasName("ix_outbox_events_dead_letter");

        // Foreign key to Property
        builder.HasOne(e => e.Property)
            .WithMany()
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("outbox_events");
    }
}
