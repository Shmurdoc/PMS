using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

public class RfidReaderConfiguration : IEntityTypeConfiguration<RfidReader>
{
    public void Configure(EntityTypeBuilder<RfidReader> builder)
    {
        builder.ToTable("rfid_readers");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(r => r.ReaderSerial).HasColumnName("reader_serial").HasMaxLength(100).IsRequired();
        builder.Property(r => r.ReaderName).HasColumnName("reader_name").HasMaxLength(100).IsRequired();
        
        builder.Property(r => r.ReaderType)
            .HasColumnName("reader_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.LocationDescription).HasColumnName("location_description");
        builder.Property(r => r.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(r => r.MacAddress).HasColumnName("mac_address").HasMaxLength(17);
        builder.Property(r => r.ApiKey).HasColumnName("api_key").HasMaxLength(100);
        builder.Property(r => r.LastSeenAt).HasColumnName("last_seen_at");

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasDefaultValue(ReaderStatus.Active);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(r => r.ReaderSerial).IsUnique();
        builder.HasIndex(r => r.ApiKey);

        // Ignore domain events
        builder.Ignore(r => r.DomainEvents);
    }
}
