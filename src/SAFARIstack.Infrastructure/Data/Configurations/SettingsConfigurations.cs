using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════
//  PROPERTY SETTINGS CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════
public class PropertySettingsConfiguration : IEntityTypeConfiguration<PropertySettings>
{
    public void Configure(EntityTypeBuilder<PropertySettings> builder)
    {
        builder.ToTable("property_settings");

        builder.HasKey(ps => ps.Id);
        builder.Property(ps => ps.Id).HasColumnName("id");

        builder.Property(ps => ps.PropertyId).HasColumnName("property_id").IsRequired();

        // Operational
        builder.Property(ps => ps.CheckInTime).HasColumnName("check_in_time");
        builder.Property(ps => ps.CheckOutTime).HasColumnName("check_out_time");
        builder.Property(ps => ps.VATRate).HasColumnName("vat_rate").HasPrecision(5, 4);
        builder.Property(ps => ps.TourismLevyRate).HasColumnName("tourism_levy_rate").HasPrecision(5, 4);
        builder.Property(ps => ps.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(3);
        builder.Property(ps => ps.Timezone).HasColumnName("timezone").HasMaxLength(50);
        builder.Property(ps => ps.MaxAdvanceBookingDays).HasColumnName("max_advance_booking_days");
        builder.Property(ps => ps.DefaultCancellationHours).HasColumnName("default_cancellation_hours");
        builder.Property(ps => ps.LateCancellationPenaltyPercent).HasColumnName("late_cancellation_penalty_percent").HasPrecision(5, 2);
        builder.Property(ps => ps.NoShowPenaltyPercent).HasColumnName("no_show_penalty_percent").HasPrecision(5, 2);

        // Email
        builder.Property(ps => ps.SmtpHost).HasColumnName("smtp_host").HasMaxLength(255);
        builder.Property(ps => ps.SmtpPort).HasColumnName("smtp_port");
        builder.Property(ps => ps.SmtpUsername).HasColumnName("smtp_username").HasMaxLength(255);
        builder.Property(ps => ps.SmtpPassword).HasColumnName("smtp_password").HasMaxLength(500);
        builder.Property(ps => ps.SmtpUseSsl).HasColumnName("smtp_use_ssl");
        builder.Property(ps => ps.SenderEmail).HasColumnName("sender_email").HasMaxLength(255);
        builder.Property(ps => ps.SenderName).HasColumnName("sender_name").HasMaxLength(200);
        builder.Property(ps => ps.ReplyToEmail).HasColumnName("reply_to_email").HasMaxLength(255);

        // Notification prefs
        builder.Property(ps => ps.SendBookingConfirmation).HasColumnName("send_booking_confirmation").HasDefaultValue(true);
        builder.Property(ps => ps.SendBookingCancellation).HasColumnName("send_booking_cancellation").HasDefaultValue(true);
        builder.Property(ps => ps.SendCheckInReminder).HasColumnName("send_check_in_reminder").HasDefaultValue(true);
        builder.Property(ps => ps.SendCheckOutReminder).HasColumnName("send_check_out_reminder").HasDefaultValue(true);
        builder.Property(ps => ps.SendPaymentReceipt).HasColumnName("send_payment_receipt").HasDefaultValue(true);
        builder.Property(ps => ps.SendInvoice).HasColumnName("send_invoice").HasDefaultValue(true);
        builder.Property(ps => ps.SendReviewRequest).HasColumnName("send_review_request").HasDefaultValue(false);
        builder.Property(ps => ps.CheckInReminderHoursBefore).HasColumnName("check_in_reminder_hours_before").HasDefaultValue(24);
        builder.Property(ps => ps.CheckOutReminderHoursBefore).HasColumnName("check_out_reminder_hours_before").HasDefaultValue(4);

        // Branding
        builder.Property(ps => ps.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        builder.Property(ps => ps.BrandPrimaryColor).HasColumnName("brand_primary_color").HasMaxLength(7);
        builder.Property(ps => ps.EmailFooterHtml).HasColumnName("email_footer_html");
        builder.Property(ps => ps.InvoiceTermsAndConditions).HasColumnName("invoice_terms_and_conditions");
        builder.Property(ps => ps.BookingTermsAndConditions).HasColumnName("booking_terms_and_conditions");

        // Audit
        builder.Property(ps => ps.CreatedAt).HasColumnName("created_at");
        builder.Property(ps => ps.UpdatedAt).HasColumnName("updated_at");
        builder.Property(ps => ps.IsDeleted).HasColumnName("is_deleted");
        builder.Property(ps => ps.DeletedAt).HasColumnName("deleted_at");
        builder.Property(ps => ps.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(ps => ps.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(ps => ps.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(ps => ps.PropertyId).IsUnique();
        builder.HasOne<Property>().WithOne().HasForeignKey<PropertySettings>(ps => ps.PropertyId);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  EMAIL TEMPLATE CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════
public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");

        builder.HasKey(et => et.Id);
        builder.Property(et => et.Id).HasColumnName("id");

        builder.Property(et => et.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(et => et.NotificationType).HasColumnName("notification_type")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(et => et.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(et => et.SubjectTemplate).HasColumnName("subject_template").HasMaxLength(500).IsRequired();
        builder.Property(et => et.BodyHtmlTemplate).HasColumnName("body_html_template").IsRequired();
        builder.Property(et => et.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // Audit
        builder.Property(et => et.CreatedAt).HasColumnName("created_at");
        builder.Property(et => et.UpdatedAt).HasColumnName("updated_at");
        builder.Property(et => et.IsDeleted).HasColumnName("is_deleted");
        builder.Property(et => et.DeletedAt).HasColumnName("deleted_at");
        builder.Property(et => et.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(et => et.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(et => et.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(et => new { et.PropertyId, et.NotificationType, et.IsActive });
        builder.HasIndex(et => et.PropertyId);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  MERCHANT CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════
public class MerchantConfigurationEntityConfiguration : IEntityTypeConfiguration<MerchantConfiguration>
{
    public void Configure(EntityTypeBuilder<MerchantConfiguration> builder)
    {
        builder.ToTable("merchant_configurations");

        builder.HasKey(mc => mc.Id);
        builder.Property(mc => mc.Id).HasColumnName("id");

        builder.Property(mc => mc.PropertyId).HasColumnName("property_id").IsRequired();
        builder.Property(mc => mc.ProviderName).HasColumnName("provider_name").HasMaxLength(100).IsRequired();
        builder.Property(mc => mc.ProviderType).HasColumnName("provider_type")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(mc => mc.MerchantId).HasColumnName("merchant_id").HasMaxLength(200);
        builder.Property(mc => mc.ApiKey).HasColumnName("api_key").HasMaxLength(500);
        builder.Property(mc => mc.ApiSecret).HasColumnName("api_secret").HasMaxLength(500);
        builder.Property(mc => mc.PassPhrase).HasColumnName("pass_phrase").HasMaxLength(500);
        builder.Property(mc => mc.WebhookUrl).HasColumnName("webhook_url").HasMaxLength(500);
        builder.Property(mc => mc.WebhookSecret).HasColumnName("webhook_secret").HasMaxLength(500);
        builder.Property(mc => mc.ReturnUrl).HasColumnName("return_url").HasMaxLength(500);
        builder.Property(mc => mc.CancelUrl).HasColumnName("cancel_url").HasMaxLength(500);
        builder.Property(mc => mc.NotifyUrl).HasColumnName("notify_url").HasMaxLength(500);
        builder.Property(mc => mc.IsLive).HasColumnName("is_live").HasDefaultValue(false);
        builder.Property(mc => mc.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(mc => mc.AdditionalConfigJson).HasColumnName("additional_config_json");

        // Audit
        builder.Property(mc => mc.CreatedAt).HasColumnName("created_at");
        builder.Property(mc => mc.UpdatedAt).HasColumnName("updated_at");
        builder.Property(mc => mc.IsDeleted).HasColumnName("is_deleted");
        builder.Property(mc => mc.DeletedAt).HasColumnName("deleted_at");
        builder.Property(mc => mc.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(mc => mc.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(mc => mc.DeletedByUserId).HasColumnName("deleted_by_user_id");

        // Indexes
        builder.HasIndex(mc => new { mc.PropertyId, mc.ProviderType, mc.IsActive });
        builder.HasIndex(mc => mc.PropertyId);
    }
}
