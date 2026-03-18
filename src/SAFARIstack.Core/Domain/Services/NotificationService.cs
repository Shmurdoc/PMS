using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.Core.Domain.Services;

/// <summary>
/// Creates and queues notifications triggered by guest/staff activities.
/// Resolves email templates per property and respects notification preferences.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IPropertySettingsRepository _settingsRepo;
    private readonly IEmailTemplateRepository _templateRepo;

    // Default fallback templates when no custom template exists
    private static readonly Dictionary<NotificationType, (string Subject, string Body)> _defaults = new()
    {
        [NotificationType.BookingConfirmation] = (
            "Booking Confirmed — {{BookingRef}}",
            "<h2>Dear {{GuestName}},</h2><p>Your booking <strong>{{BookingRef}}</strong> at {{PropertyName}} has been confirmed.</p><p>Check-in: {{CheckInDate}}<br/>Check-out: {{CheckOutDate}}<br/>Total: {{Currency}} {{Amount}}</p><p>We look forward to welcoming you!</p>"),

        [NotificationType.BookingCancellation] = (
            "Booking Cancelled — {{BookingRef}}",
            "<h2>Dear {{GuestName}},</h2><p>Your booking <strong>{{BookingRef}}</strong> at {{PropertyName}} has been cancelled.</p><p>Reason: {{Reason}}</p><p>If this was a mistake, please contact us.</p>"),

        [NotificationType.CheckInReminder] = (
            "Check-In Reminder — {{PropertyName}}",
            "<h2>Dear {{GuestName}},</h2><p>This is a friendly reminder that your check-in at {{PropertyName}} is tomorrow.</p><p>Booking: {{BookingRef}}<br/>Check-in: {{CheckInDate}}</p><p>We look forward to seeing you!</p>"),

        [NotificationType.CheckOutReminder] = (
            "Check-Out Reminder — {{PropertyName}}",
            "<h2>Dear {{GuestName}},</h2><p>This is a reminder that your check-out from {{PropertyName}} is today.</p><p>Check-out time: {{CheckOutTime}}</p><p>Thank you for staying with us!</p>"),

        [NotificationType.PaymentReceipt] = (
            "Payment Receipt — {{PaymentRef}}",
            "<h2>Dear {{GuestName}},</h2><p>We have received your payment of <strong>{{Currency}} {{Amount}}</strong>.</p><p>Payment Reference: {{PaymentRef}}<br/>Method: {{PaymentMethod}}<br/>Date: {{Date}}</p><p>Thank you!</p>"),

        [NotificationType.InvoiceSent] = (
            "Invoice {{InvoiceNumber}} — {{PropertyName}}",
            "<h2>Dear {{GuestName}},</h2><p>Please find attached your invoice <strong>{{InvoiceNumber}}</strong>.</p><p>Amount Due: {{Currency}} {{Amount}}<br/>Due Date: {{DueDate}}</p><p>Payment details are included in the invoice.</p>"),

        [NotificationType.ReviewRequest] = (
            "How was your stay at {{PropertyName}}?",
            "<h2>Dear {{GuestName}},</h2><p>Thank you for staying at {{PropertyName}}! We hope you enjoyed your visit.</p><p>We would love to hear your feedback. Please take a moment to share your experience.</p>"),

        [NotificationType.SystemAlert] = (
            "System Alert — {{PropertyName}}",
            "<p>{{AlertMessage}}</p>"),
    };

    public NotificationService(
        IUnitOfWork uow,
        IPropertySettingsRepository settingsRepo,
        IEmailTemplateRepository templateRepo)
    {
        _uow = uow;
        _settingsRepo = settingsRepo;
        _templateRepo = templateRepo;
    }

    public async Task<Notification?> QueueNotificationAsync(
        Guid propertyId,
        NotificationType type,
        NotificationChannel channel,
        string recipientAddress,
        Dictionary<string, string> templateValues,
        Guid? guestId = null,
        Guid? staffId = null,
        CancellationToken ct = default)
    {
        // Check if this notification type is enabled for the property
        if (!await IsNotificationEnabledAsync(propertyId, type, ct))
            return null;

        // Try to find a custom template for this property + type
        var template = await _templateRepo.GetActiveTemplateAsync(propertyId, type, ct);

        string subject;
        string body;

        if (template is not null)
        {
            subject = template.RenderSubject(templateValues);
            body = template.RenderBody(templateValues);
        }
        else if (_defaults.TryGetValue(type, out var defaultTemplate))
        {
            // Use fallback default template
            subject = ReplacePlaceholders(defaultTemplate.Subject, templateValues);
            body = ReplacePlaceholders(defaultTemplate.Body, templateValues);
        }
        else
        {
            // No template at all — create minimal notification
            subject = $"{type} Notification";
            body = $"<p>Notification: {type}</p>";
        }

        var notification = Notification.Create(
            propertyId, recipientAddress, channel, type, subject, body, guestId, staffId);

        await _uow.Notifications.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        return notification;
    }

    public async Task<bool> IsNotificationEnabledAsync(
        Guid propertyId, NotificationType type, CancellationToken ct = default)
    {
        var settings = await _settingsRepo.GetByPropertyIdAsync(propertyId, ct);

        // If no settings exist, all notifications are enabled by default
        if (settings is null) return true;

        return type switch
        {
            NotificationType.BookingConfirmation => settings.SendBookingConfirmation,
            NotificationType.BookingCancellation => settings.SendBookingCancellation,
            NotificationType.CheckInReminder => settings.SendCheckInReminder,
            NotificationType.CheckOutReminder => settings.SendCheckOutReminder,
            NotificationType.PaymentReceipt => settings.SendPaymentReceipt,
            NotificationType.InvoiceSent => settings.SendInvoice,
            NotificationType.ReviewRequest => settings.SendReviewRequest,
            _ => true // System alerts, maintenance alerts etc. always enabled
        };
    }

    public async Task<IReadOnlyList<Notification>> GetQueuedNotificationsAsync(
        int batchSize = 50, CancellationToken ct = default)
    {
        return await _uow.Notifications.GetQueuedAsync(batchSize, ct);
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }
}
