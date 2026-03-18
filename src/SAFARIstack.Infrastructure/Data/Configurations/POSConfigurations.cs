using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for CasualSale entity
/// Indexes: property_id + sale_date for daily reconciliation queries
/// </summary>
public class CasualSaleConfiguration : IEntityTypeConfiguration<CasualSale>
{
    public void Configure(EntityTypeBuilder<CasualSale> builder)
    {
        // Table
        builder.ToTable("casual_sales");

        // Key
        builder.HasKey(cs => cs.Id);

        // Properties
        builder.Property(cs => cs.PropertyId)
            .IsRequired()
            .HasColumnName("property_id");

        builder.Property(cs => cs.SaleDate)
            .IsRequired()
            .HasColumnName("sale_date")
            .HasColumnType("timestamp with time zone");

        builder.Property(cs => cs.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(cs => cs.Quantity)
            .IsRequired()
            .HasColumnType("numeric(10,2)")
            .HasColumnName("quantity");

        builder.Property(cs => cs.UnitPrice)
            .IsRequired()
            .HasColumnType("numeric(10,2)")
            .HasColumnName("unit_price");

        builder.Property(cs => cs.TotalAmount)
            .IsRequired()
            .HasColumnType("numeric(10,2)")
            .HasColumnName("total_amount");

        builder.Property(cs => cs.VatRate)
            .IsRequired()
            .HasColumnType("numeric(5,4)")
            .HasColumnName("vat_rate");

        builder.Property(cs => cs.VatAmount)
            .IsRequired()
            .HasColumnType("numeric(10,2)")
            .HasColumnName("vat_amount");

        builder.Property(cs => cs.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("cash")
            .HasColumnName("payment_method");

        builder.Property(cs => cs.RecordedByStaffId)
            .HasColumnName("recorded_by_id");

        builder.Property(cs => cs.Notes)
            .HasMaxLength(1000)
            .HasColumnName("notes");

        builder.Property(cs => cs.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(cs => cs.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(cs => cs.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(cs => cs.LastModifiedByUserId)
            .HasColumnName("last_modified_by_user_id");

        // Relationships
        builder.HasOne(cs => cs.Property)
            .WithMany()
            .HasForeignKey(cs => cs.PropertyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_casual_sales_property");

        // Indexes
        builder.HasIndex(cs => new { cs.PropertyId, cs.SaleDate })
            .HasDatabaseName("ix_casual_sales_property_date")
            .IsUnique(false);

        builder.HasIndex(cs => cs.PaymentMethod)
            .HasDatabaseName("ix_casual_sales_payment_method");

        builder.HasIndex(cs => cs.CreatedAt)
            .HasDatabaseName("ix_casual_sales_created_at");
    }
}

/// <summary>
/// EF Core configuration for DayEndClose entity
/// Indexes: property_id + close_date for daily summary queries
/// </summary>
public class DayEndCloseConfiguration : IEntityTypeConfiguration<DayEndClose>
{
    public void Configure(EntityTypeBuilder<DayEndClose> builder)
    {
        // Table
        builder.ToTable("day_end_closes");

        // Key
        builder.HasKey(dec => dec.Id);

        // Properties
        builder.Property(dec => dec.PropertyId)
            .IsRequired()
            .HasColumnName("property_id");

        builder.Property(dec => dec.CloseDate)
            .IsRequired()
            .HasColumnName("close_date")
            .HasColumnType("date");

        builder.Property(dec => dec.ExpectedCash)
            .IsRequired()
            .HasColumnType("numeric(12,2)")
            .HasColumnName("expected_cash");

        builder.Property(dec => dec.ActualCash)
            .IsRequired()
            .HasColumnType("numeric(12,2)")
            .HasColumnName("actual_cash");

        builder.Property(dec => dec.Notes)
            .HasMaxLength(1000)
            .HasColumnName("notes");

        builder.Property(dec => dec.ClosedByStaffId)
            .HasColumnName("closed_by_id");

        builder.Property(dec => dec.ClosedAt)
            .IsRequired()
            .HasColumnName("closed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(dec => dec.IsVerified)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_verified");

        builder.Property(dec => dec.VerifiedByStaffId)
            .HasColumnName("verified_by_id");

        builder.Property(dec => dec.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(dec => dec.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(dec => dec.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(dec => dec.LastModifiedByUserId)
            .HasColumnName("last_modified_by_user_id");

        // Relationships
        builder.HasOne(dec => dec.Property)
            .WithMany()
            .HasForeignKey(dec => dec.PropertyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_day_end_closes_property");

        // Indexes
        builder.HasIndex(dec => new { dec.PropertyId, dec.CloseDate })
            .HasDatabaseName("ix_day_end_closes_property_date")
            .IsUnique(true); // One close per property per day

        builder.HasIndex(dec => dec.IsVerified)
            .HasDatabaseName("ix_day_end_closes_is_verified");

        builder.HasIndex(dec => dec.ClosedAt)
            .HasDatabaseName("ix_day_end_closes_closed_at");
    }
}

/// <summary>
/// EF Core configuration for InventoryItem entity
/// Indexes: property_id + sku (unique), category, stock level queries
/// </summary>
public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        // Table
        builder.ToTable("inventory_items");

        // Key
        builder.HasKey(ii => ii.Id);

        // Properties
        builder.Property(ii => ii.PropertyId)
            .IsRequired()
            .HasColumnName("property_id");

        builder.Property(ii => ii.SKU)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("sku");

        builder.Property(ii => ii.Name)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("name");

        builder.Property(ii => ii.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("category");

        builder.Property(ii => ii.CurrentStock)
            .IsRequired()
            .HasColumnType("numeric(12,4)")
            .HasColumnName("current_stock");

        builder.Property(ii => ii.ReorderLevel)
            .IsRequired()
            .HasColumnType("numeric(12,4)")
            .HasColumnName("reorder_level");

        builder.Property(ii => ii.StockUnit)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("unit")
            .HasColumnName("stock_unit");

        builder.Property(ii => ii.CostPrice)
            .IsRequired()
            .HasColumnType("numeric(10,2)")
            .HasColumnName("cost_price");

        builder.Property(ii => ii.SellingPrice)
            .HasColumnType("numeric(10,2)")
            .HasColumnName("selling_price");

        builder.Property(ii => ii.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(ii => ii.LastStockCountAt)
            .HasColumnName("last_stock_count_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ii => ii.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(ii => ii.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(ii => ii.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ii => ii.LastModifiedByUserId)
            .HasColumnName("last_modified_by_user_id");

        // Relationships
        builder.HasOne(ii => ii.Property)
            .WithMany()
            .HasForeignKey(ii => ii.PropertyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_inventory_items_property");

        // Indexes
        builder.HasIndex(ii => new { ii.PropertyId, ii.SKU })
            .HasDatabaseName("uidx_inventory_items_property_sku")
            .IsUnique(true); // SKU must be unique per property

        builder.HasIndex(ii => ii.Category)
            .HasDatabaseName("ix_inventory_items_category");

        builder.HasIndex(ii => ii.IsActive)
            .HasDatabaseName("ix_inventory_items_is_active");

        builder.HasIndex(ii => ii.CurrentStock)
            .HasDatabaseName("ix_inventory_items_current_stock");

        builder.HasIndex(ii => ii.CreatedAt)
            .HasDatabaseName("ix_inventory_items_created_at");
    }
}
