using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Address)
            .HasColumnName("address")
            .IsRequired();

        builder.Property(p => p.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Province)
            .HasColumnName("province")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Country)
            .HasColumnName("country")
            .HasMaxLength(50)
            .HasDefaultValue("South Africa");

        builder.Property(p => p.CheckInTime).HasColumnName("check_in_time");
        builder.Property(p => p.CheckOutTime).HasColumnName("check_out_time");
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("ZAR");
        builder.Property(p => p.VATRate).HasColumnName("vat_rate").HasPrecision(5, 2).HasDefaultValue(0.15m);
        builder.Property(p => p.TourismLevyRate).HasColumnName("tourism_levy_rate").HasPrecision(5, 2).HasDefaultValue(0.01m);
        builder.Property(p => p.Timezone).HasColumnName("timezone").HasDefaultValue("Africa/Johannesburg");
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        // Ignore domain events collection
        builder.Ignore(p => p.DomainEvents);
    }
}
