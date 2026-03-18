using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════
//  SEASON
// ═══════════════════════════════════════════════════════════════════════
public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(30).IsRequired();

        builder.Property(s => s.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(s => s.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(s => s.PriceMultiplier).HasColumnName("price_multiplier").HasPrecision(5, 2).HasDefaultValue(1.0m);
        builder.Property(s => s.Priority).HasColumnName("priority").HasDefaultValue(0);
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // IAuditable + ISoftDeletable
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(s => s.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(s => s.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(s => s.Property)
            .WithMany()
            .HasForeignKey(s => s.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => new { s.PropertyId, s.Code }).IsUnique();
        builder.HasIndex(s => new { s.PropertyId, s.StartDate, s.EndDate });
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RATE PLAN
// ═══════════════════════════════════════════════════════════════════════
public class RatePlanConfiguration : IEntityTypeConfiguration<RatePlan>
{
    public void Configure(EntityTypeBuilder<RatePlan> builder)
    {
        builder.ToTable("rate_plans");

        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Id).HasColumnName("id");

        builder.Property(rp => rp.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(rp => rp.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(rp => rp.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(rp => rp.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(rp => rp.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(rp => rp.IncludesBreakfast).HasColumnName("includes_breakfast").HasDefaultValue(false);
        builder.Property(rp => rp.IsRefundable).HasColumnName("is_refundable").HasDefaultValue(true);
        builder.Property(rp => rp.MinimumNights).HasColumnName("min_nights");
        builder.Property(rp => rp.MaximumNights).HasColumnName("max_nights");
        builder.Property(rp => rp.MinimumAdvanceDays).HasColumnName("min_advance_days");
        builder.Property(rp => rp.MaximumAdvanceDays).HasColumnName("max_advance_days");
        builder.Property(rp => rp.CancellationPolicyId).HasColumnName("cancellation_policy_id");
        builder.Property(rp => rp.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // IAuditable + ISoftDeletable
        builder.Property(rp => rp.CreatedAt).HasColumnName("created_at");
        builder.Property(rp => rp.UpdatedAt).HasColumnName("updated_at");
        builder.Property(rp => rp.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(rp => rp.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(rp => rp.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(rp => rp.DeletedAt).HasColumnName("deleted_at");
        builder.Property(rp => rp.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(rp => rp.Property)
            .WithMany()
            .HasForeignKey(rp => rp.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.CancellationPolicy)
            .WithMany()
            .HasForeignKey(rp => rp.CancellationPolicyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(rp => rp.Rates)
            .WithOne(r => r.RatePlan)
            .HasForeignKey(r => r.RatePlanId);

        // Indexes
        builder.HasIndex(rp => new { rp.PropertyId, rp.Code }).IsUnique();

        builder.Ignore(rp => rp.DomainEvents);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RATE
// ═══════════════════════════════════════════════════════════════════════
public class RateConfiguration : IEntityTypeConfiguration<Rate>
{
    public void Configure(EntityTypeBuilder<Rate> builder)
    {
        builder.ToTable("rates");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(r => r.RoomTypeId).HasColumnName("room_type_id").IsRequired();
        builder.Property(r => r.RatePlanId).HasColumnName("rate_plan_id").IsRequired();
        builder.Property(r => r.SeasonId).HasColumnName("season_id");
        builder.Property(r => r.AmountPerNight).HasColumnName("amount_per_night").HasPrecision(10, 2).IsRequired();
        builder.Property(r => r.SingleOccupancyRate).HasColumnName("single_occupancy_rate").HasPrecision(10, 2);
        builder.Property(r => r.ExtraAdultRate).HasColumnName("extra_adult_rate").HasPrecision(10, 2);
        builder.Property(r => r.ExtraChildRate).HasColumnName("extra_child_rate").HasPrecision(10, 2);
        builder.Property(r => r.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(r => r.EffectiveTo).HasColumnName("effective_to").IsRequired();
        builder.Property(r => r.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("ZAR");
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // IAuditable + ISoftDeletable
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(r => r.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Property(r => r.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(r => r.RoomType)
            .WithMany(rt => rt.Rates)
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RatePlan)
            .WithMany(rp => rp.Rates)
            .HasForeignKey(r => r.RatePlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Season)
            .WithMany()
            .HasForeignKey(r => r.SeasonId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(r => new { r.RoomTypeId, r.RatePlanId, r.EffectiveFrom, r.EffectiveTo });
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  CANCELLATION POLICY
// ═══════════════════════════════════════════════════════════════════════
public class CancellationPolicyConfiguration : IEntityTypeConfiguration<CancellationPolicy>
{
    public void Configure(EntityTypeBuilder<CancellationPolicy> builder)
    {
        builder.ToTable("cancellation_policies");

        builder.HasKey(cp => cp.Id);
        builder.Property(cp => cp.Id).HasColumnName("id");

        builder.Property(cp => cp.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(cp => cp.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(cp => cp.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(cp => cp.FreeCancellationHours).HasColumnName("free_cancellation_hours").IsRequired();
        builder.Property(cp => cp.PenaltyPercentage).HasColumnName("penalty_percentage").HasPrecision(5, 2).IsRequired();
        builder.Property(cp => cp.NoShowPenaltyPercentage).HasColumnName("no_show_penalty_pct").HasPrecision(5, 2);
        builder.Property(cp => cp.IsDefault).HasColumnName("is_default").HasDefaultValue(false);

        // IAuditable + ISoftDeletable
        builder.Property(cp => cp.CreatedAt).HasColumnName("created_at");
        builder.Property(cp => cp.UpdatedAt).HasColumnName("updated_at");
        builder.Property(cp => cp.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(cp => cp.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(cp => cp.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(cp => cp.DeletedAt).HasColumnName("deleted_at");
        builder.Property(cp => cp.DeletedByUserId).HasColumnName("deleted_by_user_id");

        builder.HasIndex(cp => new { cp.PropertyId, cp.Name }).IsUnique();
    }
}
