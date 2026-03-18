using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");

        builder.Property(b => b.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(b => b.GuestId).HasColumnName("guest_id").IsRequired();
        builder.Property(b => b.RatePlanId).HasColumnName("rate_plan_id");
        builder.Property(b => b.BookingReference).HasColumnName("booking_reference").HasMaxLength(50).IsRequired();

        builder.Property(b => b.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(BookingSource.Direct);

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(BookingStatus.Confirmed);

        builder.Property(b => b.CheckInDate).HasColumnName("check_in_date").IsRequired();
        builder.Property(b => b.CheckOutDate).HasColumnName("check_out_date").IsRequired();
        builder.Property(b => b.ActualCheckInTime).HasColumnName("actual_check_in_time");
        builder.Property(b => b.ActualCheckOutTime).HasColumnName("actual_check_out_time");
        builder.Property(b => b.AdultCount).HasColumnName("adult_count").HasDefaultValue(2);
        builder.Property(b => b.ChildCount).HasColumnName("child_count").HasDefaultValue(0);
        builder.Property(b => b.SpecialRequests).HasColumnName("special_requests").HasMaxLength(2000);
        builder.Property(b => b.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(b => b.ExternalReference).HasColumnName("external_reference").HasMaxLength(100);

        // Financial fields
        builder.Property(b => b.SubtotalAmount).HasColumnName("subtotal_amount").HasPrecision(10, 2).HasDefaultValue(0);
        builder.Property(b => b.VATAmount).HasColumnName("vat_amount").HasPrecision(10, 2).HasDefaultValue(0);
        builder.Property(b => b.TourismLevyAmount).HasColumnName("tourism_levy").HasPrecision(10, 2).HasDefaultValue(0);
        builder.Property(b => b.AdditionalCharges).HasColumnName("additional_charges").HasPrecision(10, 2).HasDefaultValue(0);
        builder.Property(b => b.DiscountAmount).HasColumnName("discount_amount").HasPrecision(10, 2).HasDefaultValue(0);
        builder.Property(b => b.TotalAmount).HasColumnName("total_amount").HasPrecision(10, 2).IsRequired();
        builder.Property(b => b.PaidAmount).HasColumnName("paid_amount").HasPrecision(10, 2).HasDefaultValue(0);

        // Audit
        builder.Property(b => b.CheckedInByUserId).HasColumnName("checked_in_by_user_id");
        builder.Property(b => b.CheckedOutByUserId).HasColumnName("checked_out_by_user_id");
        builder.Property(b => b.CancelledByUserId).HasColumnName("cancelled_by_user_id");
        builder.Property(b => b.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);

        // IAuditable + ISoftDeletable
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(b => b.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(b => b.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Computed — Nights (not mapped)
        builder.Ignore(b => b.Nights);
        builder.Ignore(b => b.OutstandingAmount);

        // Relationships
        builder.HasOne(b => b.Property)
            .WithMany()
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Guest)
            .WithMany(g => g.Bookings)
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.RatePlan)
            .WithMany()
            .HasForeignKey(b => b.RatePlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.Folio)
            .WithOne(f => f.Booking)
            .HasForeignKey<Folio>(f => f.BookingId);

        builder.HasMany(b => b.BookingRooms)
            .WithOne(br => br.Booking)
            .HasForeignKey(br => br.BookingId);

        // Indexes
        builder.HasIndex(b => b.BookingReference).IsUnique();
        builder.HasIndex(b => b.CheckInDate);
        builder.HasIndex(b => b.CheckOutDate);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => new { b.PropertyId, b.CheckInDate, b.CheckOutDate });
        builder.HasIndex(b => b.ExternalReference);

        // Global soft-delete filter
        builder.HasQueryFilter(b => !b.IsDeleted);

        // Ignore domain events
        builder.Ignore(b => b.DomainEvents);
    }
}
