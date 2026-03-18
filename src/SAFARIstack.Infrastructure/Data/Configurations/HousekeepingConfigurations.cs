using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════
//  HOUSEKEEPING TASK
// ═══════════════════════════════════════════════════════════════════════
public class HousekeepingTaskConfiguration : IEntityTypeConfiguration<HousekeepingTask>
{
    public void Configure(EntityTypeBuilder<HousekeepingTask> builder)
    {
        builder.ToTable("housekeeping_tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(t => t.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(t => t.AssignedToStaffId).HasColumnName("assigned_to_staff_id");

        builder.Property(t => t.TaskType)
            .HasColumnName("task_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(HousekeepingPriority.Normal);

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(25)
            .HasDefaultValue(HousekeepingTaskStatus.Pending);

        builder.Property(t => t.ScheduledDate).HasColumnName("scheduled_date").IsRequired();
        builder.Property(t => t.StartedAt).HasColumnName("started_at");
        builder.Property(t => t.CompletedAt).HasColumnName("completed_at");
        builder.Property(t => t.DurationMinutes).HasColumnName("duration_minutes");
        builder.Property(t => t.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(t => t.InspectionNotes).HasColumnName("inspection_notes").HasMaxLength(1000);
        builder.Property(t => t.InspectedByStaffId).HasColumnName("inspected_by_staff_id");
        builder.Property(t => t.PassedInspection).HasColumnName("passed_inspection").HasDefaultValue(false);

        // Checklist
        builder.Property(t => t.LinenChanged).HasColumnName("linen_changed").HasDefaultValue(false);
        builder.Property(t => t.BathroomCleaned).HasColumnName("bathroom_cleaned").HasDefaultValue(false);
        builder.Property(t => t.FloorsCleaned).HasColumnName("floors_cleaned").HasDefaultValue(false);
        builder.Property(t => t.MinibarRestocked).HasColumnName("minibar_restocked").HasDefaultValue(false);
        builder.Property(t => t.AmenitiesReplenished).HasColumnName("amenities_replenished").HasDefaultValue(false);

        // IAuditable + ISoftDeletable
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(t => t.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(t => t.Room)
            .WithMany()
            .HasForeignKey(t => t.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => new { t.PropertyId, t.ScheduledDate, t.Status });
        builder.HasIndex(t => t.AssignedToStaffId);

        builder.Ignore(t => t.DomainEvents);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  AMENITY
// ═══════════════════════════════════════════════════════════════════════
public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("amenities");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Icon).HasColumnName("icon").HasMaxLength(50);

        builder.Property(a => a.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(a => new { a.PropertyId, a.Name }).IsUnique();
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ROOM TYPE AMENITY (M:M join)
// ═══════════════════════════════════════════════════════════════════════
public class RoomTypeAmenityConfiguration : IEntityTypeConfiguration<RoomTypeAmenity>
{
    public void Configure(EntityTypeBuilder<RoomTypeAmenity> builder)
    {
        builder.ToTable("room_type_amenities");

        builder.HasKey(rta => rta.Id);
        builder.Property(rta => rta.Id).HasColumnName("id");

        builder.Property(rta => rta.RoomTypeId).HasColumnName("room_type_id").IsRequired();
        builder.Property(rta => rta.AmenityId).HasColumnName("amenity_id").IsRequired();

        builder.Property(rta => rta.CreatedAt).HasColumnName("created_at");
        builder.Property(rta => rta.UpdatedAt).HasColumnName("updated_at");

        // Relationships
        builder.HasOne(rta => rta.RoomType)
            .WithMany(rt => rt.Amenities)
            .HasForeignKey(rta => rta.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rta => rta.Amenity)
            .WithMany()
            .HasForeignKey(rta => rta.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on the pair
        builder.HasIndex(rta => new { rta.RoomTypeId, rta.AmenityId }).IsUnique();
    }
}
