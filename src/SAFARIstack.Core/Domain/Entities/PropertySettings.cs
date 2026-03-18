using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
//  PROPERTY SETTINGS — Configurable operational values per establishment
//  SuperAdmin can modify these at runtime to adapt the system to each
//  property's unique requirements without code changes.
// ═══════════════════════════════════════════════════════════════════════
public class PropertySettings : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }

    // ─── Operational Values ─────────────────────────────────────────
    public TimeSpan CheckInTime { get; private set; } = new(14, 0, 0);
    public TimeSpan CheckOutTime { get; private set; } = new(10, 0, 0);
    public decimal VATRate { get; private set; } = 0.15m;
    public decimal TourismLevyRate { get; private set; } = 0.01m;
    public string DefaultCurrency { get; private set; } = "ZAR";
    public string Timezone { get; private set; } = "Africa/Johannesburg";
    public int MaxAdvanceBookingDays { get; private set; } = 365;
    public int DefaultCancellationHours { get; private set; } = 48;
    public decimal LateCancellationPenaltyPercent { get; private set; } = 50m;
    public decimal NoShowPenaltyPercent { get; private set; } = 100m;

    // ─── Email Configuration ────────────────────────────────────────
    public string? SmtpHost { get; private set; }
    public int SmtpPort { get; private set; } = 587;
    public string? SmtpUsername { get; private set; }
    public string? SmtpPassword { get; private set; }           // Encrypted at rest
    public bool SmtpUseSsl { get; private set; } = true;
    public string? SenderEmail { get; private set; }
    public string? SenderName { get; private set; }
    public string? ReplyToEmail { get; private set; }

    // ─── Notification Preferences ───────────────────────────────────
    public bool SendBookingConfirmation { get; private set; } = true;
    public bool SendBookingCancellation { get; private set; } = true;
    public bool SendCheckInReminder { get; private set; } = true;
    public bool SendCheckOutReminder { get; private set; } = true;
    public bool SendPaymentReceipt { get; private set; } = true;
    public bool SendInvoice { get; private set; } = true;
    public bool SendReviewRequest { get; private set; } = false;
    public int CheckInReminderHoursBefore { get; private set; } = 24;
    public int CheckOutReminderHoursBefore { get; private set; } = 4;

    // ─── Business Display Info ──────────────────────────────────────
    public string? LogoUrl { get; private set; }
    public string? BrandPrimaryColor { get; private set; } = "#2C5F2D";
    public string? EmailFooterHtml { get; private set; }
    public string? InvoiceTermsAndConditions { get; private set; }
    public string? BookingTermsAndConditions { get; private set; }

    private PropertySettings() { } // EF Core

    public static PropertySettings Create(Guid propertyId)
    {
        return new PropertySettings
        {
            PropertyId = propertyId,
        };
    }

    public void UpdateOperationalSettings(
        TimeSpan checkInTime,
        TimeSpan checkOutTime,
        decimal vatRate,
        decimal tourismLevyRate,
        string defaultCurrency,
        string timezone,
        int maxAdvanceBookingDays,
        int defaultCancellationHours,
        decimal lateCancellationPenaltyPercent,
        decimal noShowPenaltyPercent)
    {
        if (vatRate < 0 || vatRate > 1) throw new ArgumentException("VAT rate must be between 0 and 1");
        if (tourismLevyRate < 0 || tourismLevyRate > 1) throw new ArgumentException("Tourism levy rate must be between 0 and 1");
        if (maxAdvanceBookingDays < 1) throw new ArgumentException("Max advance booking days must be at least 1");
        if (defaultCancellationHours < 0) throw new ArgumentException("Cancellation hours cannot be negative");

        CheckInTime = checkInTime;
        CheckOutTime = checkOutTime;
        VATRate = vatRate;
        TourismLevyRate = tourismLevyRate;
        DefaultCurrency = defaultCurrency;
        Timezone = timezone;
        MaxAdvanceBookingDays = maxAdvanceBookingDays;
        DefaultCancellationHours = defaultCancellationHours;
        LateCancellationPenaltyPercent = lateCancellationPenaltyPercent;
        NoShowPenaltyPercent = noShowPenaltyPercent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmailSettings(
        string? smtpHost,
        int smtpPort,
        string? smtpUsername,
        string? smtpPassword,
        bool smtpUseSsl,
        string? senderEmail,
        string? senderName,
        string? replyToEmail)
    {
        SmtpHost = smtpHost;
        SmtpPort = smtpPort;
        SmtpUsername = smtpUsername;
        SmtpPassword = smtpPassword;
        SmtpUseSsl = smtpUseSsl;
        SenderEmail = senderEmail;
        SenderName = senderName;
        ReplyToEmail = replyToEmail;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotificationPreferences(
        bool sendBookingConfirmation,
        bool sendBookingCancellation,
        bool sendCheckInReminder,
        bool sendCheckOutReminder,
        bool sendPaymentReceipt,
        bool sendInvoice,
        bool sendReviewRequest,
        int checkInReminderHoursBefore,
        int checkOutReminderHoursBefore)
    {
        SendBookingConfirmation = sendBookingConfirmation;
        SendBookingCancellation = sendBookingCancellation;
        SendCheckInReminder = sendCheckInReminder;
        SendCheckOutReminder = sendCheckOutReminder;
        SendPaymentReceipt = sendPaymentReceipt;
        SendInvoice = sendInvoice;
        SendReviewRequest = sendReviewRequest;
        CheckInReminderHoursBefore = checkInReminderHoursBefore;
        CheckOutReminderHoursBefore = checkOutReminderHoursBefore;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBrandingSettings(
        string? logoUrl,
        string? brandPrimaryColor,
        string? emailFooterHtml,
        string? invoiceTermsAndConditions,
        string? bookingTermsAndConditions)
    {
        LogoUrl = logoUrl;
        BrandPrimaryColor = brandPrimaryColor;
        EmailFooterHtml = emailFooterHtml;
        InvoiceTermsAndConditions = invoiceTermsAndConditions;
        BookingTermsAndConditions = bookingTermsAndConditions;
        UpdatedAt = DateTime.UtcNow;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  EMAIL TEMPLATE — Customizable per-property email templates
//  Each NotificationType can have a custom Subject and Body template.
//  Supports placeholders: {{GuestName}}, {{BookingRef}}, {{Amount}}, etc.
// ═══════════════════════════════════════════════════════════════════════
public class EmailTemplate : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string SubjectTemplate { get; private set; } = string.Empty;
    public string BodyHtmlTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private EmailTemplate() { }

    public static EmailTemplate Create(
        Guid propertyId,
        NotificationType type,
        string name,
        string subjectTemplate,
        string bodyHtmlTemplate)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Template name is required");
        if (string.IsNullOrWhiteSpace(subjectTemplate)) throw new ArgumentException("Subject template is required");
        if (string.IsNullOrWhiteSpace(bodyHtmlTemplate)) throw new ArgumentException("Body template is required");

        return new EmailTemplate
        {
            PropertyId = propertyId,
            NotificationType = type,
            Name = name,
            SubjectTemplate = subjectTemplate,
            BodyHtmlTemplate = bodyHtmlTemplate,
        };
    }

    public void Update(string name, string subjectTemplate, string bodyHtmlTemplate)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Template name is required");
        if (string.IsNullOrWhiteSpace(subjectTemplate)) throw new ArgumentException("Subject template is required");
        if (string.IsNullOrWhiteSpace(bodyHtmlTemplate)) throw new ArgumentException("Body template is required");

        Name = name;
        SubjectTemplate = subjectTemplate;
        BodyHtmlTemplate = bodyHtmlTemplate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }

    /// <summary>
    /// Renders the subject template by replacing placeholders with actual values.
    /// Supported placeholders: {{GuestName}}, {{BookingRef}}, {{PropertyName}}, {{Amount}}, {{Date}}, {{InvoiceNumber}}
    /// </summary>
    public string RenderSubject(Dictionary<string, string> values)
        => ReplacePlaceholders(SubjectTemplate, values);

    /// <summary>
    /// Renders the body template by replacing placeholders with actual values.
    /// </summary>
    public string RenderBody(Dictionary<string, string> values)
        => ReplacePlaceholders(BodyHtmlTemplate, values);

    private static string ReplacePlaceholders(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  MERCHANT CONFIGURATION — Payment gateway API keys per establishment
//  Each property can add their own PayFast, Yoco, SnapScan, Ozow keys.
//  Secrets are stored encrypted at rest (application-level encryption).
// ═══════════════════════════════════════════════════════════════════════
public class MerchantConfiguration : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;           // "PayFast", "Yoco", "SnapScan", "Ozow", "Stripe", custom
    public MerchantProviderType ProviderType { get; private set; }
    public string? MerchantId { get; private set; }
    public string? ApiKey { get; private set; }                                // Encrypted
    public string? ApiSecret { get; private set; }                             // Encrypted
    public string? PassPhrase { get; private set; }                            // Encrypted (PayFast-specific)
    public string? WebhookUrl { get; private set; }
    public string? WebhookSecret { get; private set; }                         // Encrypted
    public string? ReturnUrl { get; private set; }
    public string? CancelUrl { get; private set; }
    public string? NotifyUrl { get; private set; }
    public bool IsLive { get; private set; }                                   // false = sandbox/test mode
    public bool IsActive { get; private set; } = true;
    public string? AdditionalConfigJson { get; private set; }                  // Arbitrary JSON for provider-specific settings

    private MerchantConfiguration() { }

    public static MerchantConfiguration Create(
        Guid propertyId,
        string providerName,
        MerchantProviderType providerType,
        string? merchantId,
        string? apiKey,
        string? apiSecret,
        bool isLive)
    {
        if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentException("Provider name is required");

        return new MerchantConfiguration
        {
            PropertyId = propertyId,
            ProviderName = providerName,
            ProviderType = providerType,
            MerchantId = merchantId,
            ApiKey = apiKey,
            ApiSecret = apiSecret,
            IsLive = isLive,
        };
    }

    public void UpdateCredentials(
        string? merchantId,
        string? apiKey,
        string? apiSecret,
        string? passPhrase,
        string? webhookSecret)
    {
        MerchantId = merchantId;
        ApiKey = apiKey;
        ApiSecret = apiSecret;
        PassPhrase = passPhrase;
        WebhookSecret = webhookSecret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateUrls(
        string? webhookUrl,
        string? returnUrl,
        string? cancelUrl,
        string? notifyUrl)
    {
        WebhookUrl = webhookUrl;
        ReturnUrl = returnUrl;
        CancelUrl = cancelUrl;
        NotifyUrl = notifyUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAdditionalConfig(string? json)
    {
        AdditionalConfigJson = json;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLiveMode(bool isLive) { IsLive = isLive; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
}

public enum MerchantProviderType
{
    PayFast,
    Ozow,
    Yoco,
    SnapScan,
    Zapper,
    Stripe,
    PayGate,
    Peach,
    Custom
}
