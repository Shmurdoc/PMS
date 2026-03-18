using FluentAssertions;
using Moq;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Core.Domain.Services;

namespace SAFARIstack.Tests.Unit.Domain;

// ═══════════════════════════════════════════════════════════════════════
//  PROPERTY SETTINGS TESTS
// ═══════════════════════════════════════════════════════════════════════
public class PropertySettingsTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_SetsDefaults()
    {
        var settings = PropertySettings.Create(PropertyId);
        settings.PropertyId.Should().Be(PropertyId);
        settings.CheckInTime.Should().Be(new TimeSpan(14, 0, 0));
        settings.CheckOutTime.Should().Be(new TimeSpan(10, 0, 0));
        settings.VATRate.Should().Be(0.15m);
        settings.TourismLevyRate.Should().Be(0.01m);
        settings.DefaultCurrency.Should().Be("ZAR");
        settings.Timezone.Should().Be("Africa/Johannesburg");
        settings.MaxAdvanceBookingDays.Should().Be(365);
        settings.DefaultCancellationHours.Should().Be(48);
        settings.SendBookingConfirmation.Should().BeTrue();
        settings.SendReviewRequest.Should().BeFalse();
    }

    [Fact]
    public void UpdateOperationalSettings_ValidValues_UpdatesAll()
    {
        var settings = PropertySettings.Create(PropertyId);
        settings.UpdateOperationalSettings(
            new TimeSpan(15, 0, 0), new TimeSpan(11, 0, 0),
            0.14m, 0.02m, "USD", "America/New_York",
            180, 24, 75m, 100m);

        settings.CheckInTime.Should().Be(new TimeSpan(15, 0, 0));
        settings.CheckOutTime.Should().Be(new TimeSpan(11, 0, 0));
        settings.VATRate.Should().Be(0.14m);
        settings.TourismLevyRate.Should().Be(0.02m);
        settings.DefaultCurrency.Should().Be("USD");
        settings.Timezone.Should().Be("America/New_York");
        settings.MaxAdvanceBookingDays.Should().Be(180);
        settings.DefaultCancellationHours.Should().Be(24);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void UpdateOperationalSettings_InvalidVATRate_Throws(decimal vatRate)
    {
        var settings = PropertySettings.Create(PropertyId);
        var act = () => settings.UpdateOperationalSettings(
            new TimeSpan(14, 0, 0), new TimeSpan(10, 0, 0),
            vatRate, 0.01m, "ZAR", "Africa/Johannesburg",
            365, 48, 50m, 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*VAT rate*");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void UpdateOperationalSettings_InvalidTourismLevyRate_Throws(decimal rate)
    {
        var settings = PropertySettings.Create(PropertyId);
        var act = () => settings.UpdateOperationalSettings(
            new TimeSpan(14, 0, 0), new TimeSpan(10, 0, 0),
            0.15m, rate, "ZAR", "Africa/Johannesburg",
            365, 48, 50m, 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*Tourism levy*");
    }

    [Fact]
    public void UpdateOperationalSettings_MaxAdvanceBookingDaysZero_Throws()
    {
        var settings = PropertySettings.Create(PropertyId);
        var act = () => settings.UpdateOperationalSettings(
            new TimeSpan(14, 0, 0), new TimeSpan(10, 0, 0),
            0.15m, 0.01m, "ZAR", "Africa/Johannesburg",
            0, 48, 50m, 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*Max advance booking*");
    }

    [Fact]
    public void UpdateOperationalSettings_NegativeCancellationHours_Throws()
    {
        var settings = PropertySettings.Create(PropertyId);
        var act = () => settings.UpdateOperationalSettings(
            new TimeSpan(14, 0, 0), new TimeSpan(10, 0, 0),
            0.15m, 0.01m, "ZAR", "Africa/Johannesburg",
            365, -1, 50m, 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*Cancellation hours*");
    }

    [Fact]
    public void UpdateEmailSettings_SetsAllFields()
    {
        var settings = PropertySettings.Create(PropertyId);
        settings.UpdateEmailSettings(
            "smtp.gmail.com", 465, "user@gmail.com", "secret",
            true, "noreply@safari.com", "Safari Lodge", "reply@safari.com");

        settings.SmtpHost.Should().Be("smtp.gmail.com");
        settings.SmtpPort.Should().Be(465);
        settings.SmtpUsername.Should().Be("user@gmail.com");
        settings.SmtpPassword.Should().Be("secret");
        settings.SmtpUseSsl.Should().BeTrue();
        settings.SenderEmail.Should().Be("noreply@safari.com");
        settings.SenderName.Should().Be("Safari Lodge");
        settings.ReplyToEmail.Should().Be("reply@safari.com");
    }

    [Fact]
    public void UpdateNotificationPreferences_SetsAll()
    {
        var settings = PropertySettings.Create(PropertyId);
        settings.UpdateNotificationPreferences(
            false, false, true, true, false, true, true, 12, 2);

        settings.SendBookingConfirmation.Should().BeFalse();
        settings.SendBookingCancellation.Should().BeFalse();
        settings.SendCheckInReminder.Should().BeTrue();
        settings.SendCheckOutReminder.Should().BeTrue();
        settings.SendPaymentReceipt.Should().BeFalse();
        settings.SendInvoice.Should().BeTrue();
        settings.SendReviewRequest.Should().BeTrue();
        settings.CheckInReminderHoursBefore.Should().Be(12);
        settings.CheckOutReminderHoursBefore.Should().Be(2);
    }

    [Fact]
    public void UpdateBrandingSettings_SetsAll()
    {
        var settings = PropertySettings.Create(PropertyId);
        settings.UpdateBrandingSettings(
            "https://cdn.lodge.com/logo.png", "#FF5733",
            "<p>Footer</p>", "Invoice T&C", "Booking T&C");

        settings.LogoUrl.Should().Be("https://cdn.lodge.com/logo.png");
        settings.BrandPrimaryColor.Should().Be("#FF5733");
        settings.EmailFooterHtml.Should().Be("<p>Footer</p>");
        settings.InvoiceTermsAndConditions.Should().Be("Invoice T&C");
        settings.BookingTermsAndConditions.Should().Be("Booking T&C");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  EMAIL TEMPLATE TESTS
// ═══════════════════════════════════════════════════════════════════════
public class EmailTemplateTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_SetsAllProperties()
    {
        var template = EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Booking Confirm", "Your booking {{BookingRef}}",
            "<p>Dear {{GuestName}}</p>");

        template.PropertyId.Should().Be(PropertyId);
        template.NotificationType.Should().Be(NotificationType.BookingConfirmation);
        template.Name.Should().Be("Booking Confirm");
        template.SubjectTemplate.Should().Contain("{{BookingRef}}");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "", "Subject", "<p>Body</p>");
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_EmptySubject_Throws()
    {
        var act = () => EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Test", "", "<p>Body</p>");
        act.Should().Throw<ArgumentException>().WithMessage("*Subject*");
    }

    [Fact]
    public void Create_EmptyBody_Throws()
    {
        var act = () => EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Test", "Subject", "");
        act.Should().Throw<ArgumentException>().WithMessage("*Body*");
    }

    [Fact]
    public void Update_SetsNewValues()
    {
        var template = EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Old Name", "Old Subject", "<p>Old Body</p>");

        template.Update("New Name", "New Subject", "<p>New Body</p>");

        template.Name.Should().Be("New Name");
        template.SubjectTemplate.Should().Be("New Subject");
        template.BodyHtmlTemplate.Should().Be("<p>New Body</p>");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var template = EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Test", "Subject", "<p>Body</p>");
        template.Deactivate();
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var template = EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Test", "Subject", "<p>Body</p>");
        template.Deactivate();
        template.Activate();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RenderSubject_ReplacesPlaceholders()
    {
        var template = EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Test", "Booking {{BookingRef}} for {{GuestName}}",
            "<p>Body</p>");

        var result = template.RenderSubject(new Dictionary<string, string>
        {
            ["BookingRef"] = "BK-001",
            ["GuestName"] = "John Doe"
        });

        result.Should().Be("Booking BK-001 for John Doe");
    }

    [Fact]
    public void RenderBody_ReplacesPlaceholders()
    {
        var template = EmailTemplate.Create(
            PropertyId, NotificationType.PaymentReceipt,
            "Receipt", "Receipt",
            "<p>Dear {{GuestName}}, Amount: {{Currency}} {{Amount}}</p>");

        var result = template.RenderBody(new Dictionary<string, string>
        {
            ["GuestName"] = "Jane Smith",
            ["Currency"] = "ZAR",
            ["Amount"] = "2,500.00"
        });

        result.Should().Contain("Jane Smith");
        result.Should().Contain("ZAR");
        result.Should().Contain("2,500.00");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  MERCHANT CONFIGURATION TESTS
// ═══════════════════════════════════════════════════════════════════════
public class MerchantConfigurationTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_SetsAllProperties()
    {
        var config = MerchantConfiguration.Create(
            PropertyId, "PayFast", MerchantProviderType.PayFast,
            "12345678", "api-key-123", "api-secret-456", false);

        config.PropertyId.Should().Be(PropertyId);
        config.ProviderName.Should().Be("PayFast");
        config.ProviderType.Should().Be(MerchantProviderType.PayFast);
        config.MerchantId.Should().Be("12345678");
        config.ApiKey.Should().Be("api-key-123");
        config.ApiSecret.Should().Be("api-secret-456");
        config.IsLive.Should().BeFalse();
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyProviderName_Throws()
    {
        var act = () => MerchantConfiguration.Create(
            PropertyId, "", MerchantProviderType.Custom,
            null, null, null, false);
        act.Should().Throw<ArgumentException>().WithMessage("*Provider name*");
    }

    [Fact]
    public void UpdateCredentials_SetsAll()
    {
        var config = MerchantConfiguration.Create(
            PropertyId, "PayFast", MerchantProviderType.PayFast,
            "old-id", "old-key", "old-secret", false);

        config.UpdateCredentials("new-id", "new-key", "new-secret", "passphrase123", "webhook-secret");

        config.MerchantId.Should().Be("new-id");
        config.ApiKey.Should().Be("new-key");
        config.ApiSecret.Should().Be("new-secret");
        config.PassPhrase.Should().Be("passphrase123");
        config.WebhookSecret.Should().Be("webhook-secret");
    }

    [Fact]
    public void UpdateUrls_SetsAll()
    {
        var config = MerchantConfiguration.Create(
            PropertyId, "Yoco", MerchantProviderType.Yoco,
            null, "api-key", null, false);

        config.UpdateUrls("https://hook.example.com", "https://return.example.com",
            "https://cancel.example.com", "https://notify.example.com");

        config.WebhookUrl.Should().Be("https://hook.example.com");
        config.ReturnUrl.Should().Be("https://return.example.com");
        config.CancelUrl.Should().Be("https://cancel.example.com");
        config.NotifyUrl.Should().Be("https://notify.example.com");
    }

    [Fact]
    public void SetAdditionalConfig_SetsJson()
    {
        var config = MerchantConfiguration.Create(
            PropertyId, "Custom Gateway", MerchantProviderType.Custom,
            null, null, null, false);

        config.SetAdditionalConfig("{\"custom_field\":\"value\"}");
        config.AdditionalConfigJson.Should().Be("{\"custom_field\":\"value\"}");
    }

    [Fact]
    public void SetLiveMode_Toggles()
    {
        var config = MerchantConfiguration.Create(
            PropertyId, "Stripe", MerchantProviderType.Stripe,
            null, "sk_test_123", null, false);

        config.IsLive.Should().BeFalse();
        config.SetLiveMode(true);
        config.IsLive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var config = MerchantConfiguration.Create(
            PropertyId, "SnapScan", MerchantProviderType.SnapScan,
            null, "key", null, false);
        config.Deactivate();
        config.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var config = MerchantConfiguration.Create(
            PropertyId, "Ozow", MerchantProviderType.Ozow,
            "mid", "key", "secret", false);
        config.Deactivate();
        config.Activate();
        config.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(MerchantProviderType.PayFast)]
    [InlineData(MerchantProviderType.Yoco)]
    [InlineData(MerchantProviderType.SnapScan)]
    [InlineData(MerchantProviderType.Ozow)]
    [InlineData(MerchantProviderType.Stripe)]
    [InlineData(MerchantProviderType.Zapper)]
    [InlineData(MerchantProviderType.PayGate)]
    [InlineData(MerchantProviderType.Peach)]
    [InlineData(MerchantProviderType.Custom)]
    public void Create_AllProviderTypes_Succeeds(MerchantProviderType type)
    {
        var config = MerchantConfiguration.Create(
            PropertyId, type.ToString(), type, null, "key", null, false);
        config.ProviderType.Should().Be(type);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  NOTIFICATION SERVICE TESTS (with Mocks)
// ═══════════════════════════════════════════════════════════════════════
public class NotificationServiceTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();
    private static readonly Guid GuestId = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IPropertySettingsRepository> _settingsRepoMock;
    private readonly Mock<IEmailTemplateRepository> _templateRepoMock;
    private readonly Mock<INotificationRepository> _notifRepoMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _settingsRepoMock = new Mock<IPropertySettingsRepository>();
        _templateRepoMock = new Mock<IEmailTemplateRepository>();
        _notifRepoMock = new Mock<INotificationRepository>();

        _uowMock.Setup(u => u.Notifications).Returns(_notifRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new NotificationService(_uowMock.Object, _settingsRepoMock.Object, _templateRepoMock.Object);
    }

    [Fact]
    public async Task QueueNotification_NoSettings_UsesDefaultTemplate()
    {
        _settingsRepoMock.Setup(r => r.GetByPropertyIdAsync(PropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySettings?)null);
        _templateRepoMock.Setup(r => r.GetActiveTemplateAsync(PropertyId, NotificationType.BookingConfirmation, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate?)null);

        var result = await _service.QueueNotificationAsync(
            PropertyId, NotificationType.BookingConfirmation, NotificationChannel.Email,
            "guest@test.com",
            new Dictionary<string, string>
            {
                ["GuestName"] = "John Doe",
                ["BookingRef"] = "BK-001",
                ["PropertyName"] = "Test Lodge",
                ["CheckInDate"] = "01 Feb 2026",
                ["CheckOutDate"] = "03 Feb 2026",
                ["Amount"] = "5000.00",
                ["Currency"] = "ZAR"
            },
            guestId: GuestId);

        result.Should().NotBeNull();
        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueNotification_CustomTemplate_UsesCustomTemplate()
    {
        _settingsRepoMock.Setup(r => r.GetByPropertyIdAsync(PropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySettings?)null);

        var customTemplate = EmailTemplate.Create(
            PropertyId, NotificationType.BookingConfirmation,
            "Custom Booking", "Custom: {{BookingRef}}", "<h1>Welcome {{GuestName}}</h1>");

        _templateRepoMock.Setup(r => r.GetActiveTemplateAsync(PropertyId, NotificationType.BookingConfirmation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customTemplate);

        var result = await _service.QueueNotificationAsync(
            PropertyId, NotificationType.BookingConfirmation, NotificationChannel.Email,
            "guest@test.com",
            new Dictionary<string, string>
            {
                ["GuestName"] = "Jane",
                ["BookingRef"] = "BK-002"
            },
            guestId: GuestId);

        result.Should().NotBeNull();
        _notifRepoMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n => n.Subject.Contains("BK-002")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueNotification_DisabledType_ReturnsNull()
    {
        var settings = PropertySettings.Create(PropertyId);
        settings.UpdateNotificationPreferences(
            sendBookingConfirmation: false,
            sendBookingCancellation: true,
            sendCheckInReminder: true,
            sendCheckOutReminder: true,
            sendPaymentReceipt: true,
            sendInvoice: true,
            sendReviewRequest: false,
            checkInReminderHoursBefore: 24,
            checkOutReminderHoursBefore: 4);

        _settingsRepoMock.Setup(r => r.GetByPropertyIdAsync(PropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await _service.QueueNotificationAsync(
            PropertyId, NotificationType.BookingConfirmation, NotificationChannel.Email,
            "guest@test.com", new Dictionary<string, string>(), guestId: GuestId);

        result.Should().BeNull();
        _notifRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsNotificationEnabled_NoSettings_ReturnsTrue()
    {
        _settingsRepoMock.Setup(r => r.GetByPropertyIdAsync(PropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySettings?)null);

        var result = await _service.IsNotificationEnabledAsync(PropertyId, NotificationType.BookingConfirmation);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsNotificationEnabled_DisabledReviewRequest_ReturnsFalse()
    {
        var settings = PropertySettings.Create(PropertyId);
        // Default: SendReviewRequest = false
        _settingsRepoMock.Setup(r => r.GetByPropertyIdAsync(PropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var result = await _service.IsNotificationEnabledAsync(PropertyId, NotificationType.ReviewRequest);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetQueuedNotifications_DelegatesToRepository()
    {
        var notifs = new List<Notification>();
        _notifRepoMock.Setup(r => r.GetQueuedAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifs);

        var result = await _service.GetQueuedNotificationsAsync(25);
        result.Should().BeSameAs(notifs);
    }

    [Theory]
    [InlineData(NotificationType.BookingConfirmation)]
    [InlineData(NotificationType.PaymentReceipt)]
    [InlineData(NotificationType.InvoiceSent)]
    [InlineData(NotificationType.CheckInReminder)]
    [InlineData(NotificationType.CheckOutReminder)]
    [InlineData(NotificationType.BookingCancellation)]
    [InlineData(NotificationType.ReviewRequest)]
    [InlineData(NotificationType.SystemAlert)]
    public async Task QueueNotification_AllTypes_UsesDefaultWhenNoCustom(NotificationType type)
    {
        _settingsRepoMock.Setup(r => r.GetByPropertyIdAsync(PropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySettings?)null);
        _templateRepoMock.Setup(r => r.GetActiveTemplateAsync(PropertyId, type, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate?)null);

        var result = await _service.QueueNotificationAsync(
            PropertyId, type, NotificationChannel.Email,
            "test@test.com",
            new Dictionary<string, string>
            {
                ["GuestName"] = "Test", ["BookingRef"] = "BK-X",
                ["PropertyName"] = "Lodge", ["Amount"] = "100",
                ["Currency"] = "ZAR", ["Date"] = "2026-01-01",
                ["PaymentRef"] = "PAY-X", ["PaymentMethod"] = "Card",
                ["InvoiceNumber"] = "INV-X", ["DueDate"] = "2026-02-01",
                ["CheckInDate"] = "2026-01-01", ["CheckOutDate"] = "2026-01-03",
                ["CheckOutTime"] = "10:00", ["Reason"] = "Changed plans",
                ["AlertMessage"] = "System maintenance"
            },
            guestId: GuestId);

        result.Should().NotBeNull();
    }
}
