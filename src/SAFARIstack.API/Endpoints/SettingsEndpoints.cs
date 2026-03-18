using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        // ═══════════════════════════════════════════════════════════════
        //  PROPERTY SETTINGS ENDPOINTS
        //  Allows SuperAdmin / PropertyAdmin to configure operational
        //  values, email settings, notification preferences, and branding.
        // ═══════════════════════════════════════════════════════════════
        var settingsGroup = app.MapGroup("/api/properties/{propertyId:guid}/settings")
            .WithTags("Property Settings")
            .RequireAuthorization("AdminOnly")
            .RequireTenantValidation()
            .WithAutoValidation();

        // ─── GET current settings ───────────────────────────────────
        settingsGroup.MapGet("/", async (Guid propertyId, IUnitOfWork uow) =>
        {
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                // Auto-create default settings on first access
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
                await uow.SaveChangesAsync();
            }
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithName("GetPropertySettings").WithOpenApi();

        // ─── PUT operational settings ───────────────────────────────
        settingsGroup.MapPut("/operational", async (Guid propertyId, UpdateOperationalSettingsRequest req, IUnitOfWork uow) =>
        {
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
            }

            settings.UpdateOperationalSettings(
                req.CheckInTime, req.CheckOutTime, req.VATRate, req.TourismLevyRate,
                req.DefaultCurrency, req.Timezone, req.MaxAdvanceBookingDays,
                req.DefaultCancellationHours, req.LateCancellationPenaltyPercent,
                req.NoShowPenaltyPercent);

            await uow.SaveChangesAsync();
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithName("UpdateOperationalSettings").WithOpenApi();

        // ─── PUT email settings ─────────────────────────────────────
        settingsGroup.MapPut("/email", async (Guid propertyId, UpdateEmailSettingsRequest req, IUnitOfWork uow) =>
        {
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
            }

            settings.UpdateEmailSettings(
                req.SmtpHost, req.SmtpPort, req.SmtpUsername, req.SmtpPassword,
                req.SmtpUseSsl, req.SenderEmail, req.SenderName, req.ReplyToEmail);

            await uow.SaveChangesAsync();
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithName("UpdateEmailSettings").WithOpenApi();

        // ─── PUT notification preferences ───────────────────────────
        settingsGroup.MapPut("/notifications", async (Guid propertyId, UpdateNotificationPreferencesRequest req, IUnitOfWork uow) =>
        {
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
            }

            settings.UpdateNotificationPreferences(
                req.SendBookingConfirmation, req.SendBookingCancellation,
                req.SendCheckInReminder, req.SendCheckOutReminder,
                req.SendPaymentReceipt, req.SendInvoice, req.SendReviewRequest,
                req.CheckInReminderHoursBefore, req.CheckOutReminderHoursBefore);

            await uow.SaveChangesAsync();
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithName("UpdateNotificationPreferences").WithOpenApi();

        // ─── PUT branding ───────────────────────────────────────────
        settingsGroup.MapPut("/branding", async (Guid propertyId, UpdateBrandingSettingsRequest req, IUnitOfWork uow) =>
        {
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
            }

            settings.UpdateBrandingSettings(
                req.LogoUrl, req.BrandPrimaryColor, req.EmailFooterHtml,
                req.InvoiceTermsAndConditions, req.BookingTermsAndConditions);

            await uow.SaveChangesAsync();
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithName("UpdateBrandingSettings").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  EMAIL TEMPLATE ENDPOINTS
        //  Per-property customizable email templates.
        // ═══════════════════════════════════════════════════════════════
        var templateGroup = app.MapGroup("/api/properties/{propertyId:guid}/email-templates")
            .WithTags("Email Templates")
            .RequireAuthorization("AdminOnly")
            .RequireTenantValidation()
            .WithAutoValidation();

        templateGroup.MapGet("/", async (Guid propertyId, IUnitOfWork uow) =>
        {
            var templates = await uow.EmailTemplates.GetByPropertyAsync(propertyId);
            return Results.Ok(templates.Select(t => new EmailTemplateResponse(t)));
        })
        .WithName("GetEmailTemplates").WithOpenApi();

        templateGroup.MapGet("/{templateId:guid}", async (Guid propertyId, Guid templateId, IUnitOfWork uow) =>
        {
            var template = await uow.EmailTemplates.GetByIdAsync(templateId);
            if (template is null || template.PropertyId != propertyId) return Results.NotFound();
            return Results.Ok(new EmailTemplateResponse(template));
        })
        .WithName("GetEmailTemplate").WithOpenApi();

        templateGroup.MapPost("/", async (Guid propertyId, CreateEmailTemplateRequest req, IUnitOfWork uow) =>
        {
            var template = EmailTemplate.Create(
                propertyId, req.NotificationType, req.Name,
                req.SubjectTemplate, req.BodyHtmlTemplate);

            await uow.EmailTemplates.AddAsync(template);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/properties/{propertyId}/email-templates/{template.Id}",
                new EmailTemplateResponse(template));
        })
        .WithName("CreateEmailTemplate").WithOpenApi();

        templateGroup.MapPut("/{templateId:guid}", async (Guid propertyId, Guid templateId, UpdateEmailTemplateRequest req, IUnitOfWork uow) =>
        {
            var template = await uow.EmailTemplates.GetByIdAsync(templateId);
            if (template is null || template.PropertyId != propertyId) return Results.NotFound();

            template.Update(req.Name, req.SubjectTemplate, req.BodyHtmlTemplate);
            await uow.SaveChangesAsync();
            return Results.Ok(new EmailTemplateResponse(template));
        })
        .WithName("UpdateEmailTemplate").WithOpenApi();

        templateGroup.MapPost("/{templateId:guid}/activate", async (Guid propertyId, Guid templateId, IUnitOfWork uow) =>
        {
            var template = await uow.EmailTemplates.GetByIdAsync(templateId);
            if (template is null || template.PropertyId != propertyId) return Results.NotFound();
            template.Activate();
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("ActivateEmailTemplate").WithOpenApi();

        templateGroup.MapPost("/{templateId:guid}/deactivate", async (Guid propertyId, Guid templateId, IUnitOfWork uow) =>
        {
            var template = await uow.EmailTemplates.GetByIdAsync(templateId);
            if (template is null || template.PropertyId != propertyId) return Results.NotFound();
            template.Deactivate();
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeactivateEmailTemplate").WithOpenApi();

        templateGroup.MapDelete("/{templateId:guid}", async (Guid propertyId, Guid templateId, IUnitOfWork uow) =>
        {
            var template = await uow.EmailTemplates.GetByIdAsync(templateId);
            if (template is null || template.PropertyId != propertyId) return Results.NotFound();
            uow.EmailTemplates.Remove(template);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteEmailTemplate").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  MERCHANT CONFIGURATION ENDPOINTS
        //  Per-property payment gateway (PayFast, Yoco, Ozow, etc.) keys.
        // ═══════════════════════════════════════════════════════════════
        var merchantGroup = app.MapGroup("/api/properties/{propertyId:guid}/merchant-config")
            .WithTags("Merchant Configuration")
            .RequireAuthorization("AdminOnly")
            .RequireTenantValidation()
            .WithAutoValidation();

        merchantGroup.MapGet("/", async (Guid propertyId, IUnitOfWork uow) =>
        {
            var configs = await uow.MerchantConfigurations.GetByPropertyAsync(propertyId);
            return Results.Ok(configs.Select(c => new MerchantConfigResponse(c)));
        })
        .WithName("GetMerchantConfigurations").WithOpenApi();

        merchantGroup.MapGet("/{configId:guid}", async (Guid propertyId, Guid configId, IUnitOfWork uow) =>
        {
            var config = await uow.MerchantConfigurations.GetByIdAsync(configId);
            if (config is null || config.PropertyId != propertyId) return Results.NotFound();
            return Results.Ok(new MerchantConfigResponse(config));
        })
        .WithName("GetMerchantConfiguration").WithOpenApi();

        merchantGroup.MapPost("/", async (Guid propertyId, CreateMerchantConfigRequest req, IUnitOfWork uow) =>
        {
            var config = MerchantConfiguration.Create(
                propertyId, req.ProviderName, req.ProviderType,
                req.MerchantId, req.ApiKey, req.ApiSecret, req.IsLive);

            if (req.PassPhrase is not null || req.WebhookSecret is not null)
                config.UpdateCredentials(req.MerchantId, req.ApiKey, req.ApiSecret, req.PassPhrase, req.WebhookSecret);

            if (req.WebhookUrl is not null || req.ReturnUrl is not null || req.CancelUrl is not null || req.NotifyUrl is not null)
                config.UpdateUrls(req.WebhookUrl, req.ReturnUrl, req.CancelUrl, req.NotifyUrl);

            if (req.AdditionalConfigJson is not null)
                config.SetAdditionalConfig(req.AdditionalConfigJson);

            await uow.MerchantConfigurations.AddAsync(config);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/properties/{propertyId}/merchant-config/{config.Id}",
                new MerchantConfigResponse(config));
        })
        .WithName("CreateMerchantConfig").WithOpenApi();

        merchantGroup.MapPut("/{configId:guid}/credentials", async (Guid propertyId, Guid configId, UpdateMerchantCredentialsRequest req, IUnitOfWork uow) =>
        {
            var config = await uow.MerchantConfigurations.GetByIdAsync(configId);
            if (config is null || config.PropertyId != propertyId) return Results.NotFound();

            config.UpdateCredentials(req.MerchantId, req.ApiKey, req.ApiSecret, req.PassPhrase, req.WebhookSecret);
            await uow.SaveChangesAsync();
            return Results.Ok(new MerchantConfigResponse(config));
        })
        .WithName("UpdateMerchantCredentials").WithOpenApi();

        merchantGroup.MapPut("/{configId:guid}/urls", async (Guid propertyId, Guid configId, UpdateMerchantUrlsRequest req, IUnitOfWork uow) =>
        {
            var config = await uow.MerchantConfigurations.GetByIdAsync(configId);
            if (config is null || config.PropertyId != propertyId) return Results.NotFound();

            config.UpdateUrls(req.WebhookUrl, req.ReturnUrl, req.CancelUrl, req.NotifyUrl);
            await uow.SaveChangesAsync();
            return Results.Ok(new MerchantConfigResponse(config));
        })
        .WithName("UpdateMerchantUrls").WithOpenApi();

        merchantGroup.MapPut("/{configId:guid}/mode", async (Guid propertyId, Guid configId, SetMerchantModeRequest req, IUnitOfWork uow) =>
        {
            var config = await uow.MerchantConfigurations.GetByIdAsync(configId);
            if (config is null || config.PropertyId != propertyId) return Results.NotFound();

            config.SetLiveMode(req.IsLive);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("SetMerchantMode").WithOpenApi();

        merchantGroup.MapPost("/{configId:guid}/activate", async (Guid propertyId, Guid configId, IUnitOfWork uow) =>
        {
            var config = await uow.MerchantConfigurations.GetByIdAsync(configId);
            if (config is null || config.PropertyId != propertyId) return Results.NotFound();
            config.Activate();
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("ActivateMerchantConfig").WithOpenApi();

        merchantGroup.MapPost("/{configId:guid}/deactivate", async (Guid propertyId, Guid configId, IUnitOfWork uow) =>
        {
            var config = await uow.MerchantConfigurations.GetByIdAsync(configId);
            if (config is null || config.PropertyId != propertyId) return Results.NotFound();
            config.Deactivate();
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeactivateMerchantConfig").WithOpenApi();

        merchantGroup.MapDelete("/{configId:guid}", async (Guid propertyId, Guid configId, IUnitOfWork uow) =>
        {
            var config = await uow.MerchantConfigurations.GetByIdAsync(configId);
            if (config is null || config.PropertyId != propertyId) return Results.NotFound();
            uow.MerchantConfigurations.Remove(config);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteMerchantConfig").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  NOTIFICATION ENDPOINTS
        //  View notifications for a property, mark as read/sent.
        // ═══════════════════════════════════════════════════════════════
        var notifGroup = app.MapGroup("/api/properties/{propertyId:guid}/notifications")
            .WithTags("Notifications")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        notifGroup.MapGet("/", async (Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Notifications
                .Where(n => n.PropertyId == propertyId)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .Select(n => new
                {
                    n.Id, n.Type, n.Channel, n.RecipientAddress, n.Subject,
                    n.Status, n.SentAt, n.ErrorMessage, n.CreatedAt
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetPropertyNotifications").WithOpenApi();

        notifGroup.MapGet("/queued", async (int? batchSize, INotificationService notifService) =>
        {
            var queued = await notifService.GetQueuedNotificationsAsync(batchSize ?? 50);
            return Results.Ok(queued.Select(n => new
            {
                n.Id, n.Type, n.Channel, n.RecipientAddress, n.Subject,
                n.Status, n.CreatedAt
            }));
        })
        .WithName("GetQueuedNotifications").WithOpenApi();

        notifGroup.MapPost("/{notificationId:guid}/sent", async (Guid propertyId, Guid notificationId, IUnitOfWork uow) =>
        {
            var notification = await uow.Notifications.GetByIdAsync(notificationId);
            if (notification is null || notification.PropertyId != propertyId) return Results.NotFound();
            notification.MarkSent();
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("MarkNotificationSent").WithOpenApi();

        notifGroup.MapPost("/{notificationId:guid}/failed", async (Guid propertyId, Guid notificationId, MarkFailedRequest req, IUnitOfWork uow) =>
        {
            var notification = await uow.Notifications.GetByIdAsync(notificationId);
            if (notification is null || notification.PropertyId != propertyId) return Results.NotFound();
            notification.MarkFailed(req.Reason);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("MarkNotificationFailed").WithOpenApi();

        notifGroup.MapPost("/{notificationId:guid}/read", async (Guid propertyId, Guid notificationId, IUnitOfWork uow) =>
        {
            var notification = await uow.Notifications.GetByIdAsync(notificationId);
            if (notification is null || notification.PropertyId != propertyId) return Results.NotFound();
            notification.MarkRead();
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("MarkNotificationRead").WithOpenApi();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  REQUEST / RESPONSE DTOs
// ═══════════════════════════════════════════════════════════════════

// ─── Property Settings ──────────────────────────────────────────
public record UpdateOperationalSettingsRequest(
    TimeSpan CheckInTime, TimeSpan CheckOutTime,
    decimal VATRate, decimal TourismLevyRate,
    string DefaultCurrency, string Timezone,
    int MaxAdvanceBookingDays, int DefaultCancellationHours,
    decimal LateCancellationPenaltyPercent, decimal NoShowPenaltyPercent);

public record UpdateEmailSettingsRequest(
    string? SmtpHost, int SmtpPort, string? SmtpUsername, string? SmtpPassword,
    bool SmtpUseSsl, string? SenderEmail, string? SenderName, string? ReplyToEmail);

public record UpdateNotificationPreferencesRequest(
    bool SendBookingConfirmation, bool SendBookingCancellation,
    bool SendCheckInReminder, bool SendCheckOutReminder,
    bool SendPaymentReceipt, bool SendInvoice, bool SendReviewRequest,
    int CheckInReminderHoursBefore, int CheckOutReminderHoursBefore);

public record UpdateBrandingSettingsRequest(
    string? LogoUrl, string? BrandPrimaryColor, string? EmailFooterHtml,
    string? InvoiceTermsAndConditions, string? BookingTermsAndConditions);

public record PropertySettingsResponse
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }

    // Operational
    public TimeSpan CheckInTime { get; init; }
    public TimeSpan CheckOutTime { get; init; }
    public decimal VATRate { get; init; }
    public decimal TourismLevyRate { get; init; }
    public string DefaultCurrency { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public int MaxAdvanceBookingDays { get; init; }
    public int DefaultCancellationHours { get; init; }
    public decimal LateCancellationPenaltyPercent { get; init; }
    public decimal NoShowPenaltyPercent { get; init; }

    // Email
    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; }
    public string? SmtpUsername { get; init; }
    public bool SmtpUseSsl { get; init; }
    public string? SenderEmail { get; init; }
    public string? SenderName { get; init; }
    public string? ReplyToEmail { get; init; }

    // Notification prefs
    public bool SendBookingConfirmation { get; init; }
    public bool SendBookingCancellation { get; init; }
    public bool SendCheckInReminder { get; init; }
    public bool SendCheckOutReminder { get; init; }
    public bool SendPaymentReceipt { get; init; }
    public bool SendInvoice { get; init; }
    public bool SendReviewRequest { get; init; }
    public int CheckInReminderHoursBefore { get; init; }
    public int CheckOutReminderHoursBefore { get; init; }

    // Branding
    public string? LogoUrl { get; init; }
    public string? BrandPrimaryColor { get; init; }
    public string? EmailFooterHtml { get; init; }
    public string? InvoiceTermsAndConditions { get; init; }
    public string? BookingTermsAndConditions { get; init; }

    public PropertySettingsResponse() { }

    public PropertySettingsResponse(PropertySettings ps)
    {
        Id = ps.Id; PropertyId = ps.PropertyId;
        CheckInTime = ps.CheckInTime; CheckOutTime = ps.CheckOutTime;
        VATRate = ps.VATRate; TourismLevyRate = ps.TourismLevyRate;
        DefaultCurrency = ps.DefaultCurrency; Timezone = ps.Timezone;
        MaxAdvanceBookingDays = ps.MaxAdvanceBookingDays; DefaultCancellationHours = ps.DefaultCancellationHours;
        LateCancellationPenaltyPercent = ps.LateCancellationPenaltyPercent; NoShowPenaltyPercent = ps.NoShowPenaltyPercent;
        SmtpHost = ps.SmtpHost; SmtpPort = ps.SmtpPort; SmtpUsername = ps.SmtpUsername;
        SmtpUseSsl = ps.SmtpUseSsl; SenderEmail = ps.SenderEmail; SenderName = ps.SenderName; ReplyToEmail = ps.ReplyToEmail;
        SendBookingConfirmation = ps.SendBookingConfirmation; SendBookingCancellation = ps.SendBookingCancellation;
        SendCheckInReminder = ps.SendCheckInReminder; SendCheckOutReminder = ps.SendCheckOutReminder;
        SendPaymentReceipt = ps.SendPaymentReceipt; SendInvoice = ps.SendInvoice; SendReviewRequest = ps.SendReviewRequest;
        CheckInReminderHoursBefore = ps.CheckInReminderHoursBefore; CheckOutReminderHoursBefore = ps.CheckOutReminderHoursBefore;
        LogoUrl = ps.LogoUrl; BrandPrimaryColor = ps.BrandPrimaryColor; EmailFooterHtml = ps.EmailFooterHtml;
        InvoiceTermsAndConditions = ps.InvoiceTermsAndConditions; BookingTermsAndConditions = ps.BookingTermsAndConditions;
    }
}

// ─── Email Templates ────────────────────────────────────────────
public record CreateEmailTemplateRequest(
    NotificationType NotificationType, string Name,
    string SubjectTemplate, string BodyHtmlTemplate);

public record UpdateEmailTemplateRequest(
    string Name, string SubjectTemplate, string BodyHtmlTemplate);

public record EmailTemplateResponse
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public NotificationType NotificationType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SubjectTemplate { get; init; } = string.Empty;
    public string BodyHtmlTemplate { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public EmailTemplateResponse() { }

    public EmailTemplateResponse(EmailTemplate t)
    {
        Id = t.Id; PropertyId = t.PropertyId; NotificationType = t.NotificationType;
        Name = t.Name; SubjectTemplate = t.SubjectTemplate; BodyHtmlTemplate = t.BodyHtmlTemplate;
        IsActive = t.IsActive; CreatedAt = t.CreatedAt; UpdatedAt = t.UpdatedAt;
    }
}

// ─── Merchant Configuration ────────────────────────────────────
public record CreateMerchantConfigRequest(
    string ProviderName, MerchantProviderType ProviderType,
    string? MerchantId, string? ApiKey, string? ApiSecret,
    string? PassPhrase, string? WebhookUrl, string? WebhookSecret,
    string? ReturnUrl, string? CancelUrl, string? NotifyUrl,
    bool IsLive, string? AdditionalConfigJson);

public record UpdateMerchantCredentialsRequest(
    string? MerchantId, string? ApiKey, string? ApiSecret,
    string? PassPhrase, string? WebhookSecret);

public record UpdateMerchantUrlsRequest(
    string? WebhookUrl, string? ReturnUrl, string? CancelUrl, string? NotifyUrl);

public record SetMerchantModeRequest(bool IsLive);

public record MerchantConfigResponse
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public MerchantProviderType ProviderType { get; init; }
    public string? MerchantId { get; init; }
    public bool HasApiKey { get; init; }          // Never expose actual key
    public bool HasApiSecret { get; init; }       // Never expose actual secret
    public bool HasPassPhrase { get; init; }
    public string? WebhookUrl { get; init; }
    public string? ReturnUrl { get; init; }
    public string? CancelUrl { get; init; }
    public string? NotifyUrl { get; init; }
    public bool IsLive { get; init; }
    public bool IsActive { get; init; }
    public string? AdditionalConfigJson { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public MerchantConfigResponse() { }

    public MerchantConfigResponse(MerchantConfiguration mc)
    {
        Id = mc.Id; PropertyId = mc.PropertyId;
        ProviderName = mc.ProviderName; ProviderType = mc.ProviderType;
        MerchantId = mc.MerchantId;
        HasApiKey = !string.IsNullOrEmpty(mc.ApiKey);
        HasApiSecret = !string.IsNullOrEmpty(mc.ApiSecret);
        HasPassPhrase = !string.IsNullOrEmpty(mc.PassPhrase);
        WebhookUrl = mc.WebhookUrl; ReturnUrl = mc.ReturnUrl;
        CancelUrl = mc.CancelUrl; NotifyUrl = mc.NotifyUrl;
        IsLive = mc.IsLive; IsActive = mc.IsActive;
        AdditionalConfigJson = mc.AdditionalConfigJson;
        CreatedAt = mc.CreatedAt; UpdatedAt = mc.UpdatedAt;
    }
}

// ─── Notification ───────────────────────────────────────────────
public record MarkFailedRequest(string Reason);
