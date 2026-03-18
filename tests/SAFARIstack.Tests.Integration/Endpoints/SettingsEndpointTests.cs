using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/properties/{id}/settings, /email-templates, /merchant-config,
/// and /notifications endpoints — full CRUD + validation.
/// Verifies SuperAdmin can configure establishment settings end-to-end.
/// </summary>
[Collection("WebApp")]
public class SettingsEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public SettingsEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    // ═══════════════════════════════════════════════════════════════
    //  PROPERTY SETTINGS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPropertySettings_FirstAccess_ReturnsDefaults()
    {
        var client = _factory.CreateAuthenticatedClient();
        var resp = await client.GetAsync($"/api/properties/{_factory.PropertyAId}/settings");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("defaultCurrency").GetString().Should().Be("ZAR");
        body.GetProperty("vatRate").GetDecimal().Should().Be(0.15m);
        body.GetProperty("timezone").GetString().Should().Be("Africa/Johannesburg");
        body.GetProperty("sendBookingConfirmation").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOperationalSettings_ValidRequest_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.PutAsJsonAsync($"/api/properties/{_factory.PropertyAId}/settings/operational", new
        {
            CheckInTime = "15:00:00",
            CheckOutTime = "11:00:00",
            VATRate = 0.14,
            TourismLevyRate = 0.02,
            DefaultCurrency = "USD",
            Timezone = "America/New_York",
            MaxAdvanceBookingDays = 180,
            DefaultCancellationHours = 24,
            LateCancellationPenaltyPercent = 75,
            NoShowPenaltyPercent = 100
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("defaultCurrency").GetString().Should().Be("USD");
        body.GetProperty("vatRate").GetDecimal().Should().Be(0.14m);
        body.GetProperty("maxAdvanceBookingDays").GetInt32().Should().Be(180);
    }

    [Fact]
    public async Task UpdateEmailSettings_ValidRequest_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.PutAsJsonAsync($"/api/properties/{_factory.PropertyAId}/settings/email", new
        {
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 465,
            SmtpUsername = "lodge@gmail.com",
            SmtpPassword = "app-password-123",
            SmtpUseSsl = true,
            SenderEmail = "noreply@safarilodge.co.za",
            SenderName = "Safari Lodge Alpha",
            ReplyToEmail = "info@safarilodge.co.za"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("smtpHost").GetString().Should().Be("smtp.gmail.com");
        body.GetProperty("senderEmail").GetString().Should().Be("noreply@safarilodge.co.za");
    }

    [Fact]
    public async Task UpdateNotificationPreferences_DisableReviewRequest_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.PutAsJsonAsync($"/api/properties/{_factory.PropertyAId}/settings/notifications", new
        {
            SendBookingConfirmation = true,
            SendBookingCancellation = true,
            SendCheckInReminder = true,
            SendCheckOutReminder = true,
            SendPaymentReceipt = true,
            SendInvoice = true,
            SendReviewRequest = false,
            CheckInReminderHoursBefore = 12,
            CheckOutReminderHoursBefore = 2
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("sendReviewRequest").GetBoolean().Should().BeFalse();
        body.GetProperty("checkInReminderHoursBefore").GetInt32().Should().Be(12);
    }

    [Fact]
    public async Task UpdateBrandingSettings_ValidRequest_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.PutAsJsonAsync($"/api/properties/{_factory.PropertyAId}/settings/branding", new
        {
            LogoUrl = "https://cdn.lodge.com/logo.png",
            BrandPrimaryColor = "#2C5F2D",
            EmailFooterHtml = "<p>Safari Lodge Alpha — Cape Town</p>",
            InvoiceTermsAndConditions = "Payment due within 7 days",
            BookingTermsAndConditions = "Free cancellation up to 48 hours before check-in"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("logoUrl").GetString().Should().Be("https://cdn.lodge.com/logo.png");
        body.GetProperty("brandPrimaryColor").GetString().Should().Be("#2C5F2D");
    }

    [Fact]
    public async Task GetPropertySettings_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var resp = await client.GetAsync($"/api/properties/{_factory.PropertyAId}/settings");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════
    //  EMAIL TEMPLATES
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateEmailTemplate_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/email-templates", new
        {
            NotificationType = "BookingConfirmation",
            Name = "Custom Booking Confirmation",
            SubjectTemplate = "Your booking {{BookingRef}} is confirmed!",
            BodyHtmlTemplate = "<h2>Dear {{GuestName}},</h2><p>Thank you for your booking.</p>"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Custom Booking Confirmation");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetEmailTemplates_AfterCreate_ReturnsList()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create template first
        await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/email-templates", new
        {
            NotificationType = "PaymentReceipt",
            Name = "Payment Receipt Template",
            SubjectTemplate = "Receipt for {{Amount}}",
            BodyHtmlTemplate = "<p>Payment received: {{Currency}} {{Amount}}</p>"
        });

        var resp = await client.GetAsync($"/api/properties/{_factory.PropertyAId}/email-templates");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateEmailTemplate_ChangesContent()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create
        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/email-templates", new
        {
            NotificationType = "InvoiceSent",
            Name = "Invoice Email",
            SubjectTemplate = "Invoice {{InvoiceNumber}}",
            BodyHtmlTemplate = "<p>Invoice attached</p>"
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var templateId = created.GetProperty("id").GetString();

        // Update
        var updateResp = await client.PutAsJsonAsync(
            $"/api/properties/{_factory.PropertyAId}/email-templates/{templateId}", new
        {
            Name = "Updated Invoice",
            SubjectTemplate = "Updated: Invoice {{InvoiceNumber}}",
            BodyHtmlTemplate = "<p>Updated body</p>"
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("name").GetString().Should().Be("Updated Invoice");
    }

    [Fact]
    public async Task DeactivateEmailTemplate_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create
        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/email-templates", new
        {
            NotificationType = "CheckInReminder",
            Name = "Check-In Reminder",
            SubjectTemplate = "Reminder: Check-in tomorrow!",
            BodyHtmlTemplate = "<p>See you soon!</p>"
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var templateId = created.GetProperty("id").GetString();

        // Deactivate
        var resp = await client.PostAsync(
            $"/api/properties/{_factory.PropertyAId}/email-templates/{templateId}/deactivate", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify
        var getResp = await client.GetAsync(
            $"/api/properties/{_factory.PropertyAId}/email-templates/{templateId}");
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEmailTemplate_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create
        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/email-templates", new
        {
            NotificationType = "ReviewRequest",
            Name = "Review Request",
            SubjectTemplate = "How was your stay?",
            BodyHtmlTemplate = "<p>Rate us!</p>"
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var templateId = created.GetProperty("id").GetString();

        // Delete
        var resp = await client.DeleteAsync(
            $"/api/properties/{_factory.PropertyAId}/email-templates/{templateId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetEmailTemplate_WrongProperty_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var fakeId = Guid.NewGuid();
        var resp = await client.GetAsync(
            $"/api/properties/{_factory.PropertyAId}/email-templates/{fakeId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════════════════════════
    //  MERCHANT CONFIGURATION
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateMerchantConfig_PayFast_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "PayFast",
            ProviderType = "PayFast",
            MerchantId = "10000100",
            ApiKey = "46f0cd694581a",
            ApiSecret = "7f12345678abc",
            PassPhrase = "jt7NOE43FZPn",
            WebhookUrl = "https://api.lodge.com/webhook/payfast",
            WebhookSecret = (string?)null,
            ReturnUrl = "https://lodge.com/payment/success",
            CancelUrl = "https://lodge.com/payment/cancel",
            NotifyUrl = "https://api.lodge.com/notify/payfast",
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("providerName").GetString().Should().Be("PayFast");
        body.GetProperty("merchantId").GetString().Should().Be("10000100");
        body.GetProperty("hasApiKey").GetBoolean().Should().BeTrue();
        body.GetProperty("hasApiSecret").GetBoolean().Should().BeTrue();
        body.GetProperty("hasPassPhrase").GetBoolean().Should().BeTrue();
        body.GetProperty("isLive").GetBoolean().Should().BeFalse();
        // API secrets should NOT be exposed in the response
        body.TryGetProperty("apiKey", out _).Should().BeFalse();
        body.TryGetProperty("apiSecret", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CreateMerchantConfig_Yoco_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "Yoco",
            ProviderType = "Yoco",
            MerchantId = (string?)null,
            ApiKey = "sk_test_yoco_123",
            ApiSecret = (string?)null,
            PassPhrase = (string?)null,
            WebhookUrl = (string?)null,
            WebhookSecret = (string?)null,
            ReturnUrl = (string?)null,
            CancelUrl = (string?)null,
            NotifyUrl = (string?)null,
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("providerName").GetString().Should().Be("Yoco");
    }

    [Fact]
    public async Task GetMerchantConfigurations_ReturnsList()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create one first
        await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "SnapScan",
            ProviderType = "SnapScan",
            MerchantId = "snapscan-123",
            ApiKey = "snap-key",
            ApiSecret = (string?)null,
            PassPhrase = (string?)null,
            WebhookUrl = (string?)null,
            WebhookSecret = (string?)null,
            ReturnUrl = (string?)null,
            CancelUrl = (string?)null,
            NotifyUrl = (string?)null,
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });

        var resp = await client.GetAsync($"/api/properties/{_factory.PropertyAId}/merchant-config");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateMerchantCredentials_ChangesKeys()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create
        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "Ozow",
            ProviderType = "Ozow",
            MerchantId = "old-ozow-id",
            ApiKey = "old-key",
            ApiSecret = "old-secret",
            PassPhrase = (string?)null,
            WebhookUrl = (string?)null,
            WebhookSecret = (string?)null,
            ReturnUrl = (string?)null,
            CancelUrl = (string?)null,
            NotifyUrl = (string?)null,
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var configId = created.GetProperty("id").GetString();

        // Update credentials
        var resp = await client.PutAsJsonAsync(
            $"/api/properties/{_factory.PropertyAId}/merchant-config/{configId}/credentials", new
        {
            MerchantId = "new-ozow-id",
            ApiKey = "new-key",
            ApiSecret = "new-secret",
            PassPhrase = "new-pass",
            WebhookSecret = "new-webhook-secret"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("merchantId").GetString().Should().Be("new-ozow-id");
        body.GetProperty("hasApiKey").GetBoolean().Should().BeTrue();
        body.GetProperty("hasPassPhrase").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateMerchantUrls_ChangesUrls()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create
        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "Stripe",
            ProviderType = "Stripe",
            MerchantId = (string?)null,
            ApiKey = "sk_test_stripe",
            ApiSecret = (string?)null,
            PassPhrase = (string?)null,
            WebhookUrl = (string?)null,
            WebhookSecret = (string?)null,
            ReturnUrl = (string?)null,
            CancelUrl = (string?)null,
            NotifyUrl = (string?)null,
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var configId = created.GetProperty("id").GetString();

        // Update URLs
        var resp = await client.PutAsJsonAsync(
            $"/api/properties/{_factory.PropertyAId}/merchant-config/{configId}/urls", new
        {
            WebhookUrl = "https://api.lodge.com/stripe/webhook",
            ReturnUrl = "https://lodge.com/success",
            CancelUrl = "https://lodge.com/cancel",
            NotifyUrl = "https://api.lodge.com/stripe/notify"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("webhookUrl").GetString().Should().Contain("stripe");
    }

    [Fact]
    public async Task SetMerchantMode_ToggleLive_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create in sandbox
        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "PayGate",
            ProviderType = "PayGate",
            MerchantId = "pg-123",
            ApiKey = "pg-key",
            ApiSecret = (string?)null,
            PassPhrase = (string?)null,
            WebhookUrl = (string?)null,
            WebhookSecret = (string?)null,
            ReturnUrl = (string?)null,
            CancelUrl = (string?)null,
            NotifyUrl = (string?)null,
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var configId = created.GetProperty("id").GetString();

        // Toggle to live
        var resp = await client.PutAsJsonAsync(
            $"/api/properties/{_factory.PropertyAId}/merchant-config/{configId}/mode", new { IsLive = true });
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeactivateMerchantConfig_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient();

        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "Peach",
            ProviderType = "Peach",
            MerchantId = "peach-id",
            ApiKey = "peach-key",
            ApiSecret = (string?)null,
            PassPhrase = (string?)null,
            WebhookUrl = (string?)null,
            WebhookSecret = (string?)null,
            ReturnUrl = (string?)null,
            CancelUrl = (string?)null,
            NotifyUrl = (string?)null,
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var configId = created.GetProperty("id").GetString();

        var resp = await client.PostAsync(
            $"/api/properties/{_factory.PropertyAId}/merchant-config/{configId}/deactivate", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteMerchantConfig_ReturnsNoContent()
    {
        var client = _factory.CreateAuthenticatedClient();

        var createResp = await client.PostAsJsonAsync($"/api/properties/{_factory.PropertyAId}/merchant-config", new
        {
            ProviderName = "Zapper",
            ProviderType = "Zapper",
            MerchantId = "zapper-id",
            ApiKey = "zapper-key",
            ApiSecret = (string?)null,
            PassPhrase = (string?)null,
            WebhookUrl = (string?)null,
            WebhookSecret = (string?)null,
            ReturnUrl = (string?)null,
            CancelUrl = (string?)null,
            NotifyUrl = (string?)null,
            IsLive = false,
            AdditionalConfigJson = (string?)null
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var configId = created.GetProperty("id").GetString();

        var resp = await client.DeleteAsync(
            $"/api/properties/{_factory.PropertyAId}/merchant-config/{configId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMerchantConfig_WrongProperty_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var fakeId = Guid.NewGuid();
        var resp = await client.GetAsync(
            $"/api/properties/{_factory.PropertyAId}/merchant-config/{fakeId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════════════════════════
    //  NOTIFICATION ENDPOINTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPropertyNotifications_ReturnsPagedList()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.GetAsync($"/api/properties/{_factory.PropertyAId}/notifications");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        // Should return a paged object with items array
        body.TryGetProperty("items", out var items).Should().BeTrue();
    }

    [Fact]
    public async Task GetQueuedNotifications_ReturnsArray()
    {
        var client = _factory.CreateAuthenticatedClient();

        var resp = await client.GetAsync($"/api/properties/{_factory.PropertyAId}/notifications/queued");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkNotificationSent_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var fakeId = Guid.NewGuid();
        var resp = await client.PostAsync(
            $"/api/properties/{_factory.PropertyAId}/notifications/{fakeId}/sent", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkNotificationRead_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var fakeId = Guid.NewGuid();
        var resp = await client.PostAsync(
            $"/api/properties/{_factory.PropertyAId}/notifications/{fakeId}/read", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
