using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════
//  GUEST PREFERENCE
// ═══════════════════════════════════════════════════════════════════════
public class GuestPreferenceConfiguration : IEntityTypeConfiguration<GuestPreference>
{
    public void Configure(EntityTypeBuilder<GuestPreference> builder)
    {
        builder.ToTable("guest_preferences");

        builder.HasKey(gp => gp.Id);
        builder.Property(gp => gp.Id).HasColumnName("id");

        builder.Property(gp => gp.GuestId).HasColumnName("guest_id").IsRequired();

        builder.Property(gp => gp.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(gp => gp.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(gp => gp.Value).HasColumnName("value").HasMaxLength(500).IsRequired();
        builder.Property(gp => gp.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.Property(gp => gp.CreatedAt).HasColumnName("created_at");
        builder.Property(gp => gp.UpdatedAt).HasColumnName("updated_at");

        // Relationships
        builder.HasOne(gp => gp.Guest)
            .WithMany(g => g.Preferences)
            .HasForeignKey(gp => gp.GuestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gp => new { gp.GuestId, gp.Category, gp.Key }).IsUnique();
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GUEST LOYALTY
// ═══════════════════════════════════════════════════════════════════════
public class GuestLoyaltyConfiguration : IEntityTypeConfiguration<GuestLoyalty>
{
    public void Configure(EntityTypeBuilder<GuestLoyalty> builder)
    {
        builder.ToTable("guest_loyalty");

        builder.HasKey(gl => gl.Id);
        builder.Property(gl => gl.Id).HasColumnName("id");

        builder.Property(gl => gl.GuestId).HasColumnName("guest_id").IsRequired();

        builder.Property(gl => gl.Tier)
            .HasColumnName("tier")
            .HasConversion<string>()
            .HasMaxLength(15)
            .HasDefaultValue(LoyaltyTier.None);

        builder.Property(gl => gl.TotalPoints).HasColumnName("total_points").HasDefaultValue(0);
        builder.Property(gl => gl.AvailablePoints).HasColumnName("available_points").HasDefaultValue(0);
        builder.Property(gl => gl.TotalStays).HasColumnName("total_stays").HasDefaultValue(0);
        builder.Property(gl => gl.TotalNights).HasColumnName("total_nights").HasDefaultValue(0);
        builder.Property(gl => gl.TotalSpend).HasColumnName("total_spend").HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(gl => gl.LastStayDate).HasColumnName("last_stay_date");
        builder.Property(gl => gl.TierExpiryDate).HasColumnName("tier_expiry_date");

        builder.Property(gl => gl.CreatedAt).HasColumnName("created_at");
        builder.Property(gl => gl.UpdatedAt).HasColumnName("updated_at");

        // Relationships
        builder.HasOne(gl => gl.Guest)
            .WithOne(g => g.Loyalty)
            .HasForeignKey<GuestLoyalty>(gl => gl.GuestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gl => gl.GuestId).IsUnique();
        builder.HasIndex(gl => gl.Tier);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  NOTIFICATION
// ═══════════════════════════════════════════════════════════════════════
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(n => n.RecipientGuestId).HasColumnName("recipient_guest_id");
        builder.Property(n => n.RecipientStaffId).HasColumnName("recipient_staff_id");
        builder.Property(n => n.RecipientAddress).HasColumnName("recipient_address").HasMaxLength(255).IsRequired();

        builder.Property(n => n.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.Subject).HasColumnName("subject").HasMaxLength(300).IsRequired();
        builder.Property(n => n.Body).HasColumnName("body").IsRequired();

        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(15)
            .HasDefaultValue(NotificationStatus.Queued);

        builder.Property(n => n.SentAt).HasColumnName("sent_at");
        builder.Property(n => n.ReadAt).HasColumnName("read_at");
        builder.Property(n => n.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(n => n.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
        builder.Property(n => n.ExternalReference).HasColumnName("external_reference").HasMaxLength(200);

        builder.Property(n => n.CreatedAt).HasColumnName("created_at");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => new { n.PropertyId, n.Status, n.CreatedAt });
        builder.HasIndex(n => n.RecipientGuestId);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  AUDIT LOG
// ═══════════════════════════════════════════════════════════════════════
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.PropertyId).HasColumnName("property_id");
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.UserName).HasColumnName("user_name").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(a => a.OldValues).HasColumnName("old_values");       // JSONB in Postgres
        builder.Property(a => a.NewValues).HasColumnName("new_values");       // JSONB in Postgres
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        // Indexes — designed for fast querying of audit trail
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.PropertyId);
    }
}
