using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════
//  ROOM TYPE
// ═══════════════════════════════════════════════════════════════════════
public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.ToTable("room_types");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id");

        builder.Property(rt => rt.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(rt => rt.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(rt => rt.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(rt => rt.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(rt => rt.BasePrice).HasColumnName("base_price").HasPrecision(10, 2).IsRequired();
        builder.Property(rt => rt.MaxGuests).HasColumnName("max_guests").IsRequired();
        builder.Property(rt => rt.MaxAdults).HasColumnName("max_adults").HasDefaultValue(2);
        builder.Property(rt => rt.MaxChildren).HasColumnName("max_children").HasDefaultValue(0);
        builder.Property(rt => rt.RoomCount).HasColumnName("room_count");
        builder.Property(rt => rt.SizeInSquareMeters).HasColumnName("size_sqm");
        builder.Property(rt => rt.BedConfiguration).HasColumnName("bed_configuration").HasMaxLength(100);
        builder.Property(rt => rt.ViewType).HasColumnName("view_type").HasMaxLength(50);
        builder.Property(rt => rt.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(rt => rt.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // IAuditable + ISoftDeletable
        builder.Property(rt => rt.CreatedAt).HasColumnName("created_at");
        builder.Property(rt => rt.UpdatedAt).HasColumnName("updated_at");
        builder.Property(rt => rt.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(rt => rt.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(rt => rt.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(rt => rt.DeletedAt).HasColumnName("deleted_at");
        builder.Property(rt => rt.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(rt => rt.Property)
            .WithMany()
            .HasForeignKey(rt => rt.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(rt => rt.Rooms)
            .WithOne(r => r.RoomType)
            .HasForeignKey(r => r.RoomTypeId);

        builder.HasMany(rt => rt.Rates)
            .WithOne(r => r.RoomType)
            .HasForeignKey(r => r.RoomTypeId);

        // Indexes
        builder.HasIndex(rt => new { rt.PropertyId, rt.Code }).IsUnique();

        builder.HasQueryFilter(rt => rt.IsActive && !rt.IsDeleted);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ROOM
// ═══════════════════════════════════════════════════════════════════════
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(r => r.RoomTypeId).HasColumnName("room_type_id").IsRequired();
        builder.Property(r => r.RoomNumber).HasColumnName("room_number").HasMaxLength(20).IsRequired();
        builder.Property(r => r.Floor).HasColumnName("floor");
        builder.Property(r => r.Wing).HasColumnName("wing").HasMaxLength(50);

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RoomStatus.Available);

        builder.Property(r => r.HkStatus)
            .HasColumnName("hk_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(HousekeepingStatus.Clean);

        builder.Property(r => r.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(r => r.LastCleanedAt).HasColumnName("last_cleaned_at");
        builder.Property(r => r.NextMaintenanceDate).HasColumnName("next_maintenance_date");
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
        builder.HasOne(r => r.Property)
            .WithMany()
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RoomType)
            .WithMany(rt => rt.Rooms)
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(r => new { r.PropertyId, r.RoomNumber }).IsUnique();
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.HkStatus);

        builder.HasQueryFilter(r => r.IsActive && !r.IsDeleted);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  BOOKING ROOM (M:M join table)
// ═══════════════════════════════════════════════════════════════════════
public class BookingRoomConfiguration : IEntityTypeConfiguration<BookingRoom>
{
    public void Configure(EntityTypeBuilder<BookingRoom> builder)
    {
        builder.ToTable("booking_rooms");

        builder.HasKey(br => br.Id);
        builder.Property(br => br.Id).HasColumnName("id");

        builder.Property(br => br.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(br => br.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(br => br.RoomTypeId).HasColumnName("room_type_id").IsRequired();
        builder.Property(br => br.RatePlanId).HasColumnName("rate_plan_id");
        builder.Property(br => br.RateApplied).HasColumnName("rate_applied").HasPrecision(10, 2).IsRequired();
        builder.Property(br => br.GuestNames).HasColumnName("guest_names").HasMaxLength(500);

        builder.Property(br => br.CreatedAt).HasColumnName("created_at");
        builder.Property(br => br.UpdatedAt).HasColumnName("updated_at");
        builder.Property(br => br.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(br => br.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(br => br.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(br => br.DeletedAt).HasColumnName("deleted_at");
        builder.Property(br => br.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(br => br.Booking)
            .WithMany(b => b.BookingRooms)
            .HasForeignKey(br => br.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(br => br.Room)
            .WithMany()
            .HasForeignKey(br => br.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(br => br.RoomType)
            .WithMany()
            .HasForeignKey(br => br.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(br => br.RatePlan)
            .WithMany()
            .HasForeignKey(br => br.RatePlanId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(br => new { br.BookingId, br.RoomId });
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ROOM BLOCK
// ═══════════════════════════════════════════════════════════════════════
public class RoomBlockConfiguration : IEntityTypeConfiguration<RoomBlock>
{
    public void Configure(EntityTypeBuilder<RoomBlock> builder)
    {
        builder.ToTable("room_blocks");

        builder.HasKey(rb => rb.Id);
        builder.Property(rb => rb.Id).HasColumnName("id");

        builder.Property(rb => rb.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(rb => rb.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(rb => rb.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(rb => rb.EndDate).HasColumnName("end_date").IsRequired();

        builder.Property(rb => rb.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(rb => rb.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.Property(rb => rb.CreatedAt).HasColumnName("created_at");
        builder.Property(rb => rb.UpdatedAt).HasColumnName("updated_at");
        builder.Property(rb => rb.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(rb => rb.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(rb => rb.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(rb => rb.DeletedAt).HasColumnName("deleted_at");
        builder.Property(rb => rb.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(rb => rb.Room)
            .WithMany()
            .HasForeignKey(rb => rb.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rb => new { rb.RoomId, rb.StartDate, rb.EndDate });
    }
}
