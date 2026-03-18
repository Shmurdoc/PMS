using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("guests");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");

        builder.Property(g => g.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(g => g.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(g => g.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(g => g.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(g => g.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(g => g.IdNumber).HasColumnName("id_number").HasMaxLength(20);

        builder.Property(g => g.IdType)
            .HasColumnName("id_type")
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(IdType.SAId);

        builder.Property(g => g.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(g => g.Nationality).HasColumnName("nationality").HasMaxLength(50);
        builder.Property(g => g.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(g => g.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(g => g.Province).HasColumnName("province").HasMaxLength(50);
        builder.Property(g => g.PostalCode).HasColumnName("postal_code").HasMaxLength(10);
        builder.Property(g => g.Country).HasColumnName("country").HasMaxLength(50);
        builder.Property(g => g.CompanyName).HasColumnName("company_name").HasMaxLength(200);
        builder.Property(g => g.CompanyVATNumber).HasColumnName("company_vat_number").HasMaxLength(20);

        builder.Property(g => g.GuestType)
            .HasColumnName("guest_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GuestType.Individual);

        builder.Property(g => g.MarketingOptIn).HasColumnName("marketing_opt_in").HasDefaultValue(false);
        builder.Property(g => g.IsBlacklisted).HasColumnName("is_blacklisted").HasDefaultValue(false);
        builder.Property(g => g.BlacklistReason).HasColumnName("blacklist_reason").HasMaxLength(500);
        builder.Property(g => g.Notes).HasColumnName("notes").HasMaxLength(2000);

        // IAuditable + ISoftDeletable
        builder.Property(g => g.CreatedAt).HasColumnName("created_at");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(g => g.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(g => g.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at");
        builder.Property(g => g.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Computed
        builder.Ignore(g => g.FullName);

        // Relationships
        builder.HasOne(g => g.Property)
            .WithMany()
            .HasForeignKey(g => g.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Loyalty)
            .WithOne(l => l.Guest)
            .HasForeignKey<GuestLoyalty>(l => l.GuestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Preferences)
            .WithOne(p => p.Guest)
            .HasForeignKey(p => p.GuestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(g => g.Email);
        builder.HasIndex(g => g.Phone);
        builder.HasIndex(g => new { g.PropertyId, g.Email }).IsUnique()
            .HasFilter("email IS NOT NULL AND is_deleted = false");
        builder.HasIndex(g => g.IdNumber);

        // Global soft-delete filter
        builder.HasQueryFilter(g => !g.IsDeleted);

        // Ignore domain events
        builder.Ignore(g => g.DomainEvents);
    }
}
