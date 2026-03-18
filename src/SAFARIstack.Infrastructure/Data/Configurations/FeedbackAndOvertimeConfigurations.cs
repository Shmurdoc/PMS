using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for GuestFeedback.
/// </summary>
public class GuestFeedbackConfiguration : IEntityTypeConfiguration<GuestFeedback>
{
    public void Configure(EntityTypeBuilder<GuestFeedback> builder)
    {
        builder.ToTable("guest_feedbacks");

        builder.HasKey(gf => gf.Id);
        builder.Property(gf => gf.Id).HasColumnName("id");

        // Multi-tenant
        builder.Property(gf => gf.PropertyId).HasColumnName("property_id").IsRequired();

        // Foreign keys
        builder.Property(gf => gf.BookingId).HasColumnName("booking_id");
        builder.Property(gf => gf.GuestId).HasColumnName("guest_id").IsRequired();

        // Guest info
        builder.Property(gf => gf.GuestName).HasColumnName("guest_name").HasMaxLength(255);
        builder.Property(gf => gf.GuestEmail).HasColumnName("guest_email").HasMaxLength(255);

        // Ratings
        builder.Property(gf => gf.OverallRating).HasColumnName("overall_rating").IsRequired();
        builder.Property(gf => gf.RoomCleanliness).HasColumnName("room_cleanliness");
        builder.Property(gf => gf.RoomComfort).HasColumnName("room_comfort");
        builder.Property(gf => gf.FrontDeskService).HasColumnName("front_desk_service");
        builder.Property(gf => gf.AmenityQuality).HasColumnName("amenity_quality");
        builder.Property(gf => gf.ValueForMoney).HasColumnName("value_for_money");

        // Feedback
        builder.Property(gf => gf.Comment).HasColumnName("comment").HasMaxLength(2000);
        builder.Property(gf => gf.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(20);
        builder.Property(gf => gf.Sentiment).HasColumnName("sentiment").HasConversion<string>().HasMaxLength(20);

        // Status
        builder.Property(gf => gf.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(gf => gf.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(gf => gf.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(gf => gf.ManagerResponse).HasColumnName("manager_response").HasMaxLength(2000);
        builder.Property(gf => gf.ResponseDate).HasColumnName("response_date");
        builder.Property(gf => gf.RespondedByUserId).HasColumnName("responded_by_user_id");

        // Flags
        builder.Property(gf => gf.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(gf => gf.IsPublished).HasColumnName("is_published").HasDefaultValue(false);
        builder.Property(gf => gf.RequiresAction).HasColumnName("requires_action").HasDefaultValue(false);

        // Soft delete & auditing
        builder.Property(gf => gf.IsDeleted).HasColumnName("is_deleted");
        builder.Property(gf => gf.CreatedAt).HasColumnName("created_at");
        builder.Property(gf => gf.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(gf => gf.LastModifiedByUserId).HasColumnName("modified_by_user_id");
        builder.Property(gf => gf.UpdatedAt).HasColumnName("modified_at");
        builder.Property(gf => gf.RowVersion).HasColumnName("xmin").IsRowVersion();

        // Soft delete
        builder.HasQueryFilter(gf => !gf.IsDeleted);

        // Relationships
        builder.HasOne(gf => gf.Booking)
            .WithMany()
            .HasForeignKey(gf => gf.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(gf => gf.Guest)
            .WithMany(g => g.Feedbacks)
            .HasForeignKey(gf => gf.GuestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gf => new { gf.PropertyId, gf.SubmittedAt }).IsDescending(false, true);
        builder.HasIndex(gf => new { gf.PropertyId, gf.Status });
        builder.HasIndex(gf => new { gf.PropertyId, gf.RequiresAction });
        builder.HasIndex(gf => gf.GuestId);
    }
}

/// <summary>
/// Entity configuration for OvertimeRequest.
/// </summary>
public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.ToTable("overtime_requests");

        builder.HasKey(or => or.Id);
        builder.Property(or => or.Id).HasColumnName("id");

        // Multi-tenant
        builder.Property(or => or.PropertyId).HasColumnName("property_id").IsRequired();

        // Foreign keys
        builder.Property(or => or.StaffMemberId).HasColumnName("staff_member_id").IsRequired();
        builder.Property(or => or.ApprovedByUserId).HasColumnName("approved_by_user_id");

        // Request details
        builder.Property(or => or.RequestedDate).HasColumnName("requested_date");
        builder.Property(or => or.RequestedHours).HasColumnName("requested_hours").HasPrecision(10, 2);
        builder.Property(or => or.Reason).HasColumnName("reason");

        // Status & approval
        builder.Property(or => or.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(or => or.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(or => or.ActualHoursWorked).HasColumnName("actual_hours_worked").HasPrecision(10, 2);
        builder.Property(or => or.ReviewNotes).HasColumnName("review_notes");

        // Auditing
        builder.Property(or => or.CreatedAt).HasColumnName("created_at");
        builder.Property(or => or.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(or => or.LastModifiedByUserId).HasColumnName("modified_by_user_id");
        builder.Property(or => or.UpdatedAt).HasColumnName("modified_at");
        builder.Property(or => or.RowVersion).HasColumnName("xmin").IsRowVersion();

        // Soft delete
        builder.Property(or => or.IsDeleted).HasColumnName("is_deleted");
        builder.HasQueryFilter(or => !or.IsDeleted);

        // Indexes
        builder.HasIndex(or => new { or.PropertyId, or.Status });
        builder.HasIndex(or => new { or.PropertyId, or.RequestedDate }).IsDescending(false, true);
        builder.HasIndex(or => or.StaffMemberId);
    }
}
