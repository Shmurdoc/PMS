using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════
//  FOLIO
// ═══════════════════════════════════════════════════════════════════════
public class FolioConfiguration : IEntityTypeConfiguration<Folio>
{
    public void Configure(EntityTypeBuilder<Folio> builder)
    {
        builder.ToTable("folios");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        builder.Property(f => f.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(f => f.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(f => f.GuestId).HasColumnName("guest_id").IsRequired();
        builder.Property(f => f.FolioNumber).HasColumnName("folio_number").HasMaxLength(30).IsRequired();

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(FolioStatus.Open);

        builder.Property(f => f.TotalCharges).HasColumnName("total_charges").HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(f => f.TotalPayments).HasColumnName("total_payments").HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(f => f.TotalRefunds).HasColumnName("total_refunds").HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(f => f.ClosedAt).HasColumnName("closed_at");

        // Computed property — not mapped
        builder.Ignore(f => f.Balance);

        // IAuditable + ISoftDeletable
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");
        builder.Property(f => f.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(f => f.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(f => f.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");
        builder.Property(f => f.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(f => f.Booking)
            .WithOne(b => b.Folio)
            .HasForeignKey<Folio>(f => f.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Guest)
            .WithMany()
            .HasForeignKey(f => f.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.LineItems)
            .WithOne(li => li.Folio)
            .HasForeignKey(li => li.FolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Payments)
            .WithOne(p => p.Folio)
            .HasForeignKey(p => p.FolioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(f => f.FolioNumber).IsUnique();
        builder.HasIndex(f => f.BookingId).IsUnique();
        builder.HasIndex(f => new { f.PropertyId, f.Status });

        builder.Ignore(f => f.DomainEvents);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  FOLIO LINE ITEM
// ═══════════════════════════════════════════════════════════════════════
public class FolioLineItemConfiguration : IEntityTypeConfiguration<FolioLineItem>
{
    public void Configure(EntityTypeBuilder<FolioLineItem> builder)
    {
        builder.ToTable("folio_line_items");

        builder.HasKey(li => li.Id);
        builder.Property(li => li.Id).HasColumnName("id");

        builder.Property(li => li.FolioId).HasColumnName("folio_id").IsRequired();
        builder.Property(li => li.Description).HasColumnName("description").HasMaxLength(300).IsRequired();

        builder.Property(li => li.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(li => li.Quantity).HasColumnName("quantity").HasDefaultValue(1);
        builder.Property(li => li.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2).IsRequired();
        builder.Property(li => li.ServiceItemId).HasColumnName("service_item_id");
        builder.Property(li => li.ChargeDate).HasColumnName("charge_date").IsRequired();
        builder.Property(li => li.IsVoided).HasColumnName("is_voided").HasDefaultValue(false);
        builder.Property(li => li.VoidReason).HasColumnName("void_reason").HasMaxLength(500);

        // Computed — not mapped
        builder.Ignore(li => li.TotalAmount);
        builder.Ignore(li => li.VATAmount);

        // IAuditable + ISoftDeletable
        builder.Property(li => li.CreatedAt).HasColumnName("created_at");
        builder.Property(li => li.UpdatedAt).HasColumnName("updated_at");
        builder.Property(li => li.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(li => li.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(li => li.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(li => li.DeletedAt).HasColumnName("deleted_at");
        builder.Property(li => li.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(li => li.Folio)
            .WithMany(f => f.LineItems)
            .HasForeignKey(li => li.FolioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(li => li.FolioId);
        builder.HasIndex(li => li.ChargeDate);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  PAYMENT
// ═══════════════════════════════════════════════════════════════════════
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(p => p.FolioId).HasColumnName("folio_id").IsRequired();
        builder.Property(p => p.BookingId).HasColumnName("booking_id");
        builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("ZAR");

        builder.Property(p => p.Method)
            .HasColumnName("method")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Completed);

        builder.Property(p => p.TransactionReference).HasColumnName("transaction_reference").HasMaxLength(100);
        builder.Property(p => p.GatewayReference).HasColumnName("gateway_reference").HasMaxLength(200);
        builder.Property(p => p.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(p => p.PaymentDate).HasColumnName("payment_date").IsRequired();

        // Refund
        builder.Property(p => p.IsRefund).HasColumnName("is_refund").HasDefaultValue(false);
        builder.Property(p => p.OriginalPaymentId).HasColumnName("original_payment_id");
        builder.Property(p => p.RefundReason).HasColumnName("refund_reason").HasMaxLength(500);

        // IAuditable + ISoftDeletable
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(p => p.Folio)
            .WithMany(f => f.Payments)
            .HasForeignKey(p => p.FolioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.TransactionReference);
        builder.HasIndex(p => new { p.PropertyId, p.PaymentDate });
        builder.HasIndex(p => p.FolioId);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  INVOICE
// ═══════════════════════════════════════════════════════════════════════
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(i => i.FolioId).HasColumnName("folio_id").IsRequired();
        builder.Property(i => i.GuestId).HasColumnName("guest_id").IsRequired();
        builder.Property(i => i.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(30).IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(InvoiceStatus.Draft);

        builder.Property(i => i.InvoiceDate).HasColumnName("invoice_date").IsRequired();
        builder.Property(i => i.DueDate).HasColumnName("due_date").IsRequired();
        builder.Property(i => i.SubtotalAmount).HasColumnName("subtotal_amount").HasPrecision(12, 2).IsRequired();
        builder.Property(i => i.VATAmount).HasColumnName("vat_amount").HasPrecision(12, 2).IsRequired();
        builder.Property(i => i.TourismLevyAmount).HasColumnName("tourism_levy_amount").HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(i => i.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2).IsRequired();
        builder.Property(i => i.PaidAmount).HasColumnName("paid_amount").HasPrecision(12, 2).HasDefaultValue(0);

        // SA Tax compliance
        builder.Property(i => i.VATNumber).HasColumnName("vat_number").HasMaxLength(20);
        builder.Property(i => i.CompanyName).HasColumnName("company_name").HasMaxLength(200);
        builder.Property(i => i.CompanyVATNumber).HasColumnName("company_vat_number").HasMaxLength(20);

        // Computed
        builder.Ignore(i => i.OutstandingAmount);

        // IAuditable + ISoftDeletable
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Property(i => i.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(i => i.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(i => i.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.Property(i => i.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Relationships
        builder.HasOne(i => i.Folio)
            .WithMany()
            .HasForeignKey(i => i.FolioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Guest)
            .WithMany()
            .HasForeignKey(i => i.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => new { i.PropertyId, i.Status });
        builder.HasIndex(i => i.DueDate);

        builder.Ignore(i => i.DomainEvents);
    }
}
