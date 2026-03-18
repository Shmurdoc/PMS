using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

public class StaffAttendanceConfiguration : IEntityTypeConfiguration<StaffAttendance>
{
    public void Configure(EntityTypeBuilder<StaffAttendance> builder)
    {
        builder.ToTable("staff_attendance");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.StaffId).HasColumnName("staff_id").IsRequired();
        builder.Property(a => a.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(a => a.CardUid).HasColumnName("card_uid").HasMaxLength(20).IsRequired();
        
        builder.Property(a => a.CheckInTime).HasColumnName("check_in_time").IsRequired();
        builder.Property(a => a.CheckOutTime).HasColumnName("check_out_time");
        builder.Property(a => a.ReaderId).HasColumnName("reader_id");
        builder.Property(a => a.LocationType).HasColumnName("location_type").HasMaxLength(50);

        builder.Property(a => a.ShiftType)
            .HasColumnName("shift_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.ScheduledHours).HasColumnName("scheduled_hours").HasPrecision(4, 2).IsRequired();
        builder.Property(a => a.ActualHours).HasColumnName("actual_hours").HasPrecision(5, 2);
        builder.Property(a => a.BreakStart).HasColumnName("break_start");
        builder.Property(a => a.BreakEnd).HasColumnName("break_end");
        builder.Property(a => a.BreakDuration).HasColumnName("break_duration").HasPrecision(4, 2).HasDefaultValue(0);
        
        builder.Property(a => a.OvertimeHours).HasColumnName("overtime_hours").HasPrecision(5, 2).HasDefaultValue(0);
        builder.Property(a => a.HourlyRate).HasColumnName("hourly_rate").HasPrecision(10, 2).IsRequired();
        builder.Property(a => a.OvertimeRate).HasColumnName("overtime_rate").HasPrecision(10, 2).IsRequired();
        builder.Property(a => a.TotalWage).HasColumnName("total_wage").HasPrecision(10, 2).HasDefaultValue(0);

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasDefaultValue(AttendanceStatus.CheckedIn);

        builder.Property(a => a.CheckInLatitude).HasColumnName("check_in_lat").HasPrecision(10, 8);
        builder.Property(a => a.CheckInLongitude).HasColumnName("check_in_lng").HasPrecision(11, 8);
        builder.Property(a => a.CheckOutLatitude).HasColumnName("check_out_lat").HasPrecision(10, 8);
        builder.Property(a => a.CheckOutLongitude).HasColumnName("check_out_lng").HasPrecision(11, 8);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        // Relationships
        builder.HasOne(a => a.StaffMember)
            .WithMany()
            .HasForeignKey(a => a.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Reader)
            .WithMany()
            .HasForeignKey(a => a.ReaderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(a => new { a.StaffId, a.CheckInTime });
        builder.HasIndex(a => new { a.PropertyId, a.CheckInTime });
        builder.HasIndex(a => a.CardUid);

        // Ignore domain events
        builder.Ignore(a => a.DomainEvents);
    }
}
