using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════
//  UPSELL ENGINE
// ═══════════════════════════════════════════════════════════════════════

public class UpsellOfferConfiguration : IEntityTypeConfiguration<UpsellOffer>
{
    public void Configure(EntityTypeBuilder<UpsellOffer> builder)
    {
        builder.ToTable("upsell_offers");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(u => u.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(u => u.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(u => u.OfferType)
            .HasColumnName("offer_type")
            .HasConversion<string>()
            .HasMaxLength(30);

        // Pricing
        builder.Property(u => u.OriginalPrice).HasColumnName("original_price").HasPrecision(12, 2);
        builder.Property(u => u.OfferPrice).HasColumnName("offer_price").HasPrecision(12, 2);
        builder.Property(u => u.CostPrice).HasColumnName("cost_price").HasPrecision(12, 2).HasDefaultValue(0m);

        // Inventory
        builder.Property(u => u.InventoryTotal).HasColumnName("inventory_total");
        builder.Property(u => u.InventoryRemaining).HasColumnName("inventory_remaining");

        // Validity
        builder.Property(u => u.ValidFrom).HasColumnName("valid_from");
        builder.Property(u => u.ValidTo).HasColumnName("valid_to");

        // Targeting
        builder.Property(u => u.MinNights).HasColumnName("min_nights");
        builder.Property(u => u.MinLoyaltyTier).HasColumnName("min_loyalty_tier").HasMaxLength(20);
        builder.Property(u => u.GuestType).HasColumnName("guest_type").HasMaxLength(50);
        builder.Property(u => u.BookingSource).HasColumnName("booking_source").HasMaxLength(50);
        builder.Property(u => u.ApplicableDays).HasColumnName("applicable_days").HasMaxLength(50);
        builder.Property(u => u.MaxDaysBeforeArrival).HasColumnName("max_days_before_arrival");

        // Computed — not mapped
        builder.Ignore(u => u.Savings);

        // Backing field — transactions collection
        builder.HasMany(u => u.Transactions)
            .WithOne(t => t.Offer)
            .HasForeignKey(t => t.OfferId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);

        // IAuditable + ISoftDeletable
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(u => u.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(u => u.PropertyId).HasDatabaseName("ix_upsell_offers_property_id");
        builder.HasIndex(u => new { u.PropertyId, u.IsActive }).HasDatabaseName("ix_upsell_offers_property_active");
        builder.HasIndex(u => u.OfferType).HasDatabaseName("ix_upsell_offers_type");
    }
}

public class UpsellTransactionConfiguration : IEntityTypeConfiguration<UpsellTransaction>
{
    public void Configure(EntityTypeBuilder<UpsellTransaction> builder)
    {
        builder.ToTable("upsell_transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.OfferId).HasColumnName("offer_id").IsRequired();
        builder.Property(t => t.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(t => t.GuestId).HasColumnName("guest_id").IsRequired();
        builder.Property(t => t.Quantity).HasColumnName("quantity").HasDefaultValue(1);
        builder.Property(t => t.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
        builder.Property(t => t.AddedToFolio).HasColumnName("added_to_folio").HasDefaultValue(false);
        builder.Property(t => t.FolioId).HasColumnName("folio_id");
        builder.Property(t => t.RedeemedAt).HasColumnName("redeemed_at");
        builder.Property(t => t.RedemptionNotes).HasColumnName("redemption_notes").HasMaxLength(500);

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(UpsellTransactionStatus.Purchased);

        // Computed — not mapped
        builder.Ignore(t => t.TotalAmount);

        // Navigation
        builder.HasOne(t => t.Booking)
            .WithMany()
            .HasForeignKey(t => t.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Guest)
            .WithMany()
            .HasForeignKey(t => t.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        // IAuditable + ISoftDeletable
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(t => t.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(t => t.OfferId).HasDatabaseName("ix_upsell_tx_offer_id");
        builder.HasIndex(t => t.BookingId).HasDatabaseName("ix_upsell_tx_booking_id");
        builder.HasIndex(t => t.GuestId).HasDatabaseName("ix_upsell_tx_guest_id");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GIFT CARDS
// ═══════════════════════════════════════════════════════════════════════

public class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable("gift_cards");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");

        builder.Property(g => g.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(g => g.CardNumber).HasColumnName("card_number").HasMaxLength(25).IsRequired();
        builder.Property(g => g.PinHash).HasColumnName("pin_hash").HasMaxLength(100).IsRequired();
        builder.Property(g => g.InitialBalance).HasColumnName("initial_balance").HasPrecision(12, 2);
        builder.Property(g => g.CurrentBalance).HasColumnName("current_balance").HasPrecision(12, 2);
        builder.Property(g => g.Currency).HasColumnName("currency").HasMaxLength(5).HasDefaultValue("ZAR");
        builder.Property(g => g.RecipientName).HasColumnName("recipient_name").HasMaxLength(200);
        builder.Property(g => g.RecipientEmail).HasColumnName("recipient_email").HasMaxLength(200);
        builder.Property(g => g.SenderName).HasColumnName("sender_name").HasMaxLength(200);
        builder.Property(g => g.SenderEmail).HasColumnName("sender_email").HasMaxLength(200);
        builder.Property(g => g.ScheduledDeliveryDate).HasColumnName("scheduled_delivery_date");
        builder.Property(g => g.ExpiryDate).HasColumnName("expiry_date");
        builder.Property(g => g.DesignTemplate).HasColumnName("design_template").HasMaxLength(100);
        builder.Property(g => g.PersonalMessage).HasColumnName("personal_message").HasMaxLength(500);
        builder.Property(g => g.IsMultiPropertyRedeemable).HasColumnName("is_multi_property").HasDefaultValue(true);

        builder.Property(g => g.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GiftCardStatus.Active);

        // Backing field — redemptions collection
        builder.HasMany(g => g.Redemptions)
            .WithOne(r => r.GiftCard)
            .HasForeignKey(r => r.GiftCardId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Redemptions).UsePropertyAccessMode(PropertyAccessMode.Field);

        // IAuditable + ISoftDeletable
        builder.Property(g => g.CreatedAt).HasColumnName("created_at");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(g => g.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(g => g.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at");
        builder.Property(g => g.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(g => g.CardNumber).IsUnique().HasDatabaseName("ix_gift_cards_card_number");
        builder.HasIndex(g => g.PropertyId).HasDatabaseName("ix_gift_cards_property_id");
        builder.HasIndex(g => g.Status).HasDatabaseName("ix_gift_cards_status");
        builder.HasIndex(g => g.RecipientEmail).HasDatabaseName("ix_gift_cards_recipient_email");
    }
}

public class GiftCardRedemptionConfiguration : IEntityTypeConfiguration<GiftCardRedemption>
{
    public void Configure(EntityTypeBuilder<GiftCardRedemption> builder)
    {
        builder.ToTable("gift_card_redemptions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.GiftCardId).HasColumnName("gift_card_id").IsRequired();
        builder.Property(r => r.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(r => r.BookingId).HasColumnName("booking_id");
        builder.Property(r => r.FolioId).HasColumnName("folio_id");
        builder.Property(r => r.Amount).HasColumnName("amount").HasPrecision(12, 2);
        builder.Property(r => r.RemainingBalance).HasColumnName("remaining_balance").HasPrecision(12, 2);
        builder.Property(r => r.ReceiptSent).HasColumnName("receipt_sent").HasDefaultValue(false);

        // Timestamps (Entity base)
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(r => r.GiftCardId).HasDatabaseName("ix_gc_redemptions_card_id");
        builder.HasIndex(r => r.PropertyId).HasDatabaseName("ix_gc_redemptions_property_id");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  EXPERIENCE & ACTIVITY BOOKING
// ═══════════════════════════════════════════════════════════════════════

public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("experiences");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000);
        builder.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
        builder.Property(e => e.MinGuests).HasColumnName("min_guests").HasDefaultValue(1);
        builder.Property(e => e.MaxGuests).HasColumnName("max_guests");
        builder.Property(e => e.MinAge).HasColumnName("min_age");
        builder.Property(e => e.BasePrice).HasColumnName("base_price").HasPrecision(12, 2);
        builder.Property(e => e.PricePerPerson).HasColumnName("price_per_person").HasDefaultValue(true);
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(300);
        builder.Property(e => e.IncludedItems).HasColumnName("included_items").HasMaxLength(2000);
        builder.Property(e => e.ExcludedItems).HasColumnName("excluded_items").HasMaxLength(2000);
        builder.Property(e => e.WhatToBring).HasColumnName("what_to_bring").HasMaxLength(2000);
        builder.Property(e => e.CancellationHours).HasColumnName("cancellation_hours").HasDefaultValue(24);
        builder.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.IsThirdParty).HasColumnName("is_third_party").HasDefaultValue(false);
        builder.Property(e => e.ThirdPartyOperator).HasColumnName("third_party_operator").HasMaxLength(200);
        builder.Property(e => e.CommissionRate).HasColumnName("commission_rate").HasPrecision(5, 4);

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.DifficultyLevel)
            .HasColumnName("difficulty_level")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DifficultyLevel.Easy);

        // Backing field — schedules collection
        builder.HasMany(e => e.Schedules)
            .WithOne(s => s.Experience)
            .HasForeignKey(s => s.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Schedules).UsePropertyAccessMode(PropertyAccessMode.Field);

        // IAuditable + ISoftDeletable
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(e => e.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(e => e.PropertyId).HasDatabaseName("ix_experiences_property_id");
        builder.HasIndex(e => new { e.PropertyId, e.IsActive }).HasDatabaseName("ix_experiences_property_active");
        builder.HasIndex(e => e.Category).HasDatabaseName("ix_experiences_category");
    }
}

public class ExperienceScheduleConfiguration : IEntityTypeConfiguration<ExperienceSchedule>
{
    public void Configure(EntityTypeBuilder<ExperienceSchedule> builder)
    {
        builder.ToTable("experience_schedules");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.ExperienceId).HasColumnName("experience_id").IsRequired();
        builder.Property(s => s.StartTime).HasColumnName("start_time");
        builder.Property(s => s.EndTime).HasColumnName("end_time");
        builder.Property(s => s.MaxCapacity).HasColumnName("max_capacity");
        builder.Property(s => s.GuideStaffId).HasColumnName("guide_staff_id");
        builder.Property(s => s.VehicleId).HasColumnName("vehicle_id").HasMaxLength(100);
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // int[] stored as jsonb in PostgreSQL
        builder.Property(s => s.DaysOfWeek)
            .HasColumnName("days_of_week")
            .HasColumnType("jsonb");

        // Timestamps (Entity base)
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(s => s.ExperienceId).HasDatabaseName("ix_exp_schedules_experience_id");
    }
}

public class ExperienceBookingConfiguration : IEntityTypeConfiguration<ExperienceBooking>
{
    public void Configure(EntityTypeBuilder<ExperienceBooking> builder)
    {
        builder.ToTable("experience_bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");

        builder.Property(b => b.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(b => b.GuestId).HasColumnName("guest_id").IsRequired();
        builder.Property(b => b.ExperienceId).HasColumnName("experience_id").IsRequired();
        builder.Property(b => b.BookingId).HasColumnName("booking_id");
        builder.Property(b => b.ScheduleId).HasColumnName("schedule_id");
        builder.Property(b => b.ScheduledDate).HasColumnName("scheduled_date");
        builder.Property(b => b.ScheduledTime).HasColumnName("scheduled_time");
        builder.Property(b => b.ParticipantCount).HasColumnName("participant_count");
        builder.Property(b => b.TotalPrice).HasColumnName("total_price").HasPrecision(12, 2);
        builder.Property(b => b.CommissionAmount).HasColumnName("commission_amount").HasPrecision(12, 2);
        builder.Property(b => b.CommissionRate).HasColumnName("commission_rate").HasPrecision(5, 4);
        builder.Property(b => b.SpecialRequests).HasColumnName("special_requests").HasMaxLength(2000);
        builder.Property(b => b.AssignedGuideId).HasColumnName("assigned_guide_id");
        builder.Property(b => b.CheckInTime).HasColumnName("check_in_time");
        builder.Property(b => b.CompletedAt).HasColumnName("completed_at");
        builder.Property(b => b.FeedbackScore).HasColumnName("feedback_score");
        builder.Property(b => b.FeedbackNotes).HasColumnName("feedback_notes").HasMaxLength(2000);
        builder.Property(b => b.FolioId).HasColumnName("folio_id");
        builder.Property(b => b.AddedToFolio).HasColumnName("added_to_folio").HasDefaultValue(false);

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExperienceBookingStatus.Confirmed);

        // Navigation
        builder.HasOne(b => b.Experience)
            .WithMany()
            .HasForeignKey(b => b.ExperienceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Guest)
            .WithMany()
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.HotelBooking)
            .WithMany()
            .HasForeignKey(b => b.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        // IAuditable + ISoftDeletable
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(b => b.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(b => b.PropertyId).HasDatabaseName("ix_exp_bookings_property_id");
        builder.HasIndex(b => b.ExperienceId).HasDatabaseName("ix_exp_bookings_experience_id");
        builder.HasIndex(b => b.GuestId).HasDatabaseName("ix_exp_bookings_guest_id");
        builder.HasIndex(b => new { b.ExperienceId, b.ScheduledDate }).HasDatabaseName("ix_exp_bookings_date");
        builder.HasIndex(b => b.Status).HasDatabaseName("ix_exp_bookings_status");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  MULTI-PROPERTY ENTERPRISE
// ═══════════════════════════════════════════════════════════════════════

public class PropertyGroupConfiguration : IEntityTypeConfiguration<PropertyGroup>
{
    public void Configure(EntityTypeBuilder<PropertyGroup> builder)
    {
        builder.ToTable("property_groups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");

        builder.Property(g => g.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(g => g.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(g => g.HeadquartersAddress).HasColumnName("headquarters_address").HasMaxLength(500);
        builder.Property(g => g.PrimaryContactUserId).HasColumnName("primary_contact_user_id");
        builder.Property(g => g.BillingCycle).HasColumnName("billing_cycle").HasMaxLength(20).HasDefaultValue("monthly");
        builder.Property(g => g.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        builder.Property(g => g.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // Backing field — memberships collection
        builder.HasMany(g => g.Memberships)
            .WithOne(m => m.Group)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Memberships).UsePropertyAccessMode(PropertyAccessMode.Field);

        // IAuditable + ISoftDeletable
        builder.Property(g => g.CreatedAt).HasColumnName("created_at");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(g => g.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(g => g.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at");
        builder.Property(g => g.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(g => g.Name).IsUnique().HasDatabaseName("ix_property_groups_name");
    }
}

public class PropertyGroupMembershipConfiguration : IEntityTypeConfiguration<PropertyGroupMembership>
{
    public void Configure(EntityTypeBuilder<PropertyGroupMembership> builder)
    {
        builder.ToTable("property_group_memberships");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(m => m.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(m => m.IsFlagship).HasColumnName("is_flagship").HasDefaultValue(false);
        builder.Property(m => m.JoinDate).HasColumnName("join_date");

        // Navigation
        builder.HasOne(m => m.Property)
            .WithMany()
            .HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Timestamps (Entity base)
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(m => new { m.GroupId, m.PropertyId }).IsUnique().HasDatabaseName("ix_pg_memberships_group_property");
        builder.HasIndex(m => m.PropertyId).HasDatabaseName("ix_pg_memberships_property_id");
    }
}

public class RateCopyJobConfiguration : IEntityTypeConfiguration<RateCopyJob>
{
    public void Configure(EntityTypeBuilder<RateCopyJob> builder)
    {
        builder.ToTable("rate_copy_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id");

        builder.Property(j => j.SourcePropertyId).HasColumnName("source_property_id").IsRequired();
        builder.Property(j => j.ExecutedByUserId).HasColumnName("executed_by_user_id").IsRequired();
        builder.Property(j => j.RateAdjustmentPercentage).HasColumnName("rate_adjustment_pct").HasPrecision(8, 4).HasDefaultValue(0m);
        builder.Property(j => j.OverrideExisting).HasColumnName("override_existing").HasDefaultValue(false);
        builder.Property(j => j.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(j => j.EffectiveTo).HasColumnName("effective_to");
        builder.Property(j => j.TotalRatesCopied).HasColumnName("total_rates_copied").HasDefaultValue(0);
        builder.Property(j => j.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at");

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RateCopyJobStatus.Pending);

        // List<Guid> stored as jsonb in PostgreSQL
        builder.Property(j => j.TargetPropertyIds)
            .HasColumnName("target_property_ids")
            .HasColumnType("jsonb");

        builder.Property(j => j.RatePlanIds)
            .HasColumnName("rate_plan_ids")
            .HasColumnType("jsonb");

        builder.Property(j => j.SeasonIds)
            .HasColumnName("season_ids")
            .HasColumnType("jsonb");

        // IAuditable + ISoftDeletable
        builder.Property(j => j.CreatedAt).HasColumnName("created_at");
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at");
        builder.Property(j => j.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(j => j.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(j => j.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(j => j.DeletedAt).HasColumnName("deleted_at");
        builder.Property(j => j.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(j => j.SourcePropertyId).HasDatabaseName("ix_rate_copy_jobs_source");
        builder.HasIndex(j => j.Status).HasDatabaseName("ix_rate_copy_jobs_status");
    }
}

public class GroupInventoryAllocationConfiguration : IEntityTypeConfiguration<GroupInventoryAllocation>
{
    public void Configure(EntityTypeBuilder<GroupInventoryAllocation> builder)
    {
        builder.ToTable("group_inventory_allocations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(a => a.RoomTypeId).HasColumnName("room_type_id").IsRequired();
        builder.Property(a => a.StartDate).HasColumnName("start_date");
        builder.Property(a => a.EndDate).HasColumnName("end_date");
        builder.Property(a => a.AllocatedRooms).HasColumnName("allocated_rooms");
        builder.Property(a => a.SellLimitPerProperty).HasColumnName("sell_limit_per_property");
        builder.Property(a => a.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // Navigation
        builder.HasOne(a => a.Group)
            .WithMany()
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.RoomType)
            .WithMany()
            .HasForeignKey(a => a.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // IAuditable + ISoftDeletable
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(a => a.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(a => a.GroupId).HasDatabaseName("ix_group_inv_alloc_group_id");
        builder.HasIndex(a => new { a.GroupId, a.RoomTypeId, a.StartDate }).HasDatabaseName("ix_group_inv_alloc_composite");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GUEST MESSAGING
// ═══════════════════════════════════════════════════════════════════════

public class GuestMessageConfiguration : IEntityTypeConfiguration<GuestMessage>
{
    public void Configure(EntityTypeBuilder<GuestMessage> builder)
    {
        builder.ToTable("guest_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(m => m.GuestId).HasColumnName("guest_id");
        builder.Property(m => m.BookingId).HasColumnName("booking_id");
        builder.Property(m => m.ConversationId).HasColumnName("conversation_id");
        builder.Property(m => m.SenderAddress).HasColumnName("sender_address").HasMaxLength(300).IsRequired();
        builder.Property(m => m.SenderName).HasColumnName("sender_name").HasMaxLength(200);
        builder.Property(m => m.Subject).HasColumnName("subject").HasMaxLength(500).IsRequired();
        builder.Property(m => m.Body).HasColumnName("body").IsRequired();
        builder.Property(m => m.ExternalReference).HasColumnName("external_reference").HasMaxLength(200);
        builder.Property(m => m.IsRead).HasColumnName("is_read").HasDefaultValue(false);
        builder.Property(m => m.ReadAt).HasColumnName("read_at");
        builder.Property(m => m.ReadByStaffId).HasColumnName("read_by_staff_id");
        builder.Property(m => m.AssignedToStaffId).HasColumnName("assigned_to_staff_id");
        builder.Property(m => m.FinalReply).HasColumnName("final_reply");
        builder.Property(m => m.RepliedAt).HasColumnName("replied_at");
        builder.Property(m => m.RepliedByStaffId).HasColumnName("replied_by_staff_id");

        // AI fields
        builder.Property(m => m.AiSuggestedReply).HasColumnName("ai_suggested_reply");
        builder.Property(m => m.AiConfidenceScore).HasColumnName("ai_confidence_score").HasPrecision(5, 4);
        builder.Property(m => m.AiReplyApproved).HasColumnName("ai_reply_approved").HasDefaultValue(false);
        builder.Property(m => m.AiReplyEdited).HasColumnName("ai_reply_edited").HasDefaultValue(false);
        builder.Property(m => m.SentimentScore).HasColumnName("sentiment_score").HasPrecision(5, 4);

        // Enums
        builder.Property(m => m.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.Direction)
            .HasColumnName("direction")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(MessageStatus.Received);

        builder.Property(m => m.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(MessagePriority.Normal);

        builder.Property(m => m.DetectedIntent)
            .HasColumnName("detected_intent")
            .HasConversion<string>()
            .HasMaxLength(30);

        // Navigation
        builder.HasOne(m => m.Guest)
            .WithMany()
            .HasForeignKey(m => m.GuestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Booking)
            .WithMany()
            .HasForeignKey(m => m.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        // IAuditable + ISoftDeletable
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.Property(m => m.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(m => m.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(m => m.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        builder.Property(m => m.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(m => m.PropertyId).HasDatabaseName("ix_guest_messages_property_id");
        builder.HasIndex(m => m.ConversationId).HasDatabaseName("ix_guest_messages_conversation_id");
        builder.HasIndex(m => m.GuestId).HasDatabaseName("ix_guest_messages_guest_id");
        builder.HasIndex(m => new { m.PropertyId, m.Status }).HasDatabaseName("ix_guest_messages_property_status");
        builder.HasIndex(m => m.Channel).HasDatabaseName("ix_guest_messages_channel");
    }
}

public class GuestConversationConfiguration : IEntityTypeConfiguration<GuestConversation>
{
    public void Configure(EntityTypeBuilder<GuestConversation> builder)
    {
        builder.ToTable("guest_conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(c => c.GuestId).HasColumnName("guest_id");
        builder.Property(c => c.BookingId).HasColumnName("booking_id");
        builder.Property(c => c.Subject).HasColumnName("subject").HasMaxLength(500).IsRequired();
        builder.Property(c => c.AssignedToStaffId).HasColumnName("assigned_to_staff_id");
        builder.Property(c => c.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(c => c.MessageCount).HasColumnName("message_count").HasDefaultValue(0);
        builder.Property(c => c.LastMessageAt).HasColumnName("last_message_at");

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ConversationStatus.Open);

        builder.Property(c => c.PrimaryChannel)
            .HasColumnName("primary_channel")
            .HasConversion<string>()
            .HasMaxLength(30);

        // Backing field — messages navigated from GuestMessage side
        builder.Navigation(c => c.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);

        // IAuditable + ISoftDeletable
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(c => c.PropertyId).HasDatabaseName("ix_guest_conv_property_id");
        builder.HasIndex(c => new { c.PropertyId, c.Status }).HasDatabaseName("ix_guest_conv_property_status");
        builder.HasIndex(c => c.GuestId).HasDatabaseName("ix_guest_conv_guest_id");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  AI CONCIERGE
// ═══════════════════════════════════════════════════════════════════════

public class AiInteractionConfiguration : IEntityTypeConfiguration<AiInteraction>
{
    public void Configure(EntityTypeBuilder<AiInteraction> builder)
    {
        builder.ToTable("ai_interactions");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(a => a.GuestId).HasColumnName("guest_id");
        builder.Property(a => a.BookingId).HasColumnName("booking_id");
        builder.Property(a => a.Query).HasColumnName("query").IsRequired();
        builder.Property(a => a.Response).HasColumnName("response").IsRequired();
        builder.Property(a => a.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 4);
        builder.Property(a => a.IntentCategory).HasColumnName("intent_category").HasMaxLength(50);
        builder.Property(a => a.WasApproved).HasColumnName("was_approved").HasDefaultValue(false);
        builder.Property(a => a.WasEdited).HasColumnName("was_edited").HasDefaultValue(false);
        builder.Property(a => a.EditedResponse).HasColumnName("edited_response");
        builder.Property(a => a.ReviewedByStaffId).HasColumnName("reviewed_by_staff_id");
        builder.Property(a => a.GuestSatisfaction).HasColumnName("guest_satisfaction");
        builder.Property(a => a.ProcessingTimeMs).HasColumnName("processing_time_ms");
        builder.Property(a => a.TokensUsed).HasColumnName("tokens_used").HasDefaultValue(0);
        builder.Property(a => a.Cost).HasColumnName("cost").HasPrecision(10, 6).HasDefaultValue(0m);
        builder.Property(a => a.ModelUsed).HasColumnName("model_used").HasMaxLength(100);

        builder.Property(a => a.Outcome)
            .HasColumnName("outcome")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AiInteractionOutcome.Pending);

        builder.Property(a => a.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20);

        // Navigation
        builder.HasOne(a => a.Guest)
            .WithMany()
            .HasForeignKey(a => a.GuestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Booking)
            .WithMany()
            .HasForeignKey(a => a.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        // IAuditable + ISoftDeletable
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(a => a.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(a => a.PropertyId).HasDatabaseName("ix_ai_interactions_property_id");
        builder.HasIndex(a => new { a.PropertyId, a.CreatedAt }).HasDatabaseName("ix_ai_interactions_property_date");
        builder.HasIndex(a => a.IntentCategory).HasDatabaseName("ix_ai_interactions_intent");
        builder.HasIndex(a => a.Outcome).HasDatabaseName("ix_ai_interactions_outcome");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  DIGITAL CHECK-IN
// ═══════════════════════════════════════════════════════════════════════

public class DigitalCheckInConfiguration : IEntityTypeConfiguration<DigitalCheckIn>
{
    public void Configure(EntityTypeBuilder<DigitalCheckIn> builder)
    {
        builder.ToTable("digital_check_ins");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(d => d.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(d => d.GuestId).HasColumnName("guest_id").IsRequired();
        builder.Property(d => d.Token).HasColumnName("token").HasMaxLength(100).IsRequired();
        builder.Property(d => d.TokenExpiry).HasColumnName("token_expiry");

        // Identity Verification
        builder.Property(d => d.IdVerified).HasColumnName("id_verified").HasDefaultValue(false);
        builder.Property(d => d.IdDocumentType).HasColumnName("id_document_type").HasMaxLength(50);
        builder.Property(d => d.IdDocumentHash).HasColumnName("id_document_hash").HasMaxLength(100);
        builder.Property(d => d.IdVerificationConfidence).HasColumnName("id_verification_confidence").HasPrecision(5, 4);
        builder.Property(d => d.IdVerifiedAt).HasColumnName("id_verified_at");

        // Registration Card
        builder.Property(d => d.SignatureData).HasColumnName("signature_data");
        builder.Property(d => d.SignedAt).HasColumnName("signed_at");
        builder.Property(d => d.SignedFromIpAddress).HasColumnName("signed_from_ip").HasMaxLength(50);
        builder.Property(d => d.ConsentVersion).HasColumnName("consent_version").HasMaxLength(30);
        builder.Property(d => d.PopiaConsentGiven).HasColumnName("popia_consent").HasDefaultValue(false);
        builder.Property(d => d.MarketingConsentGiven).HasColumnName("marketing_consent").HasDefaultValue(false);

        // Room Selection
        builder.Property(d => d.SelectedRoomId).HasColumnName("selected_room_id");
        builder.Property(d => d.RoomUpgradeSelected).HasColumnName("room_upgrade_selected").HasDefaultValue(false);
        builder.Property(d => d.UpgradeAmount).HasColumnName("upgrade_amount").HasPrecision(12, 2);

        // Mobile Key
        builder.Property(d => d.MobileKeyId).HasColumnName("mobile_key_id").HasMaxLength(100);
        builder.Property(d => d.MobileKeyValidFrom).HasColumnName("mobile_key_valid_from");
        builder.Property(d => d.MobileKeyValidTo).HasColumnName("mobile_key_valid_to");

        builder.Property(d => d.CompletedAt).HasColumnName("completed_at");

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DigitalCheckInStatus.Invited);

        builder.Property(d => d.MobileKeyStatus)
            .HasColumnName("mobile_key_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(MobileKeyStatus.NotProvisioned);

        // Navigation
        builder.HasOne(d => d.Booking)
            .WithMany()
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Guest)
            .WithMany()
            .HasForeignKey(d => d.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        // IAuditable + ISoftDeletable
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(d => d.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(d => d.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");
        builder.Property(d => d.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(d => d.Token).IsUnique().HasDatabaseName("ix_digital_checkins_token");
        builder.HasIndex(d => d.BookingId).HasDatabaseName("ix_digital_checkins_booking_id");
        builder.HasIndex(d => d.GuestId).HasDatabaseName("ix_digital_checkins_guest_id");
        builder.HasIndex(d => new { d.PropertyId, d.Status }).HasDatabaseName("ix_digital_checkins_property_status");
    }
}
