using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SAFARIstack.Infrastructure.Services.Notifications;

/// <summary>
/// Notification type enum for different notification categories
/// </summary>
public enum NotificationType
{
    BookingConfirmation,
    BookingCancellation,
    CheckInReminder,
    CheckOutReminder,
    PaymentConfirmation,
    RefundConfirmation,
    FeedbackRequest,
    StaffAttendanceAlert,
    RoomMaintenanceAlert,
    SystemAlert
}

/// <summary>
/// Notification request - combines email and SMS
/// </summary>
public record NotificationRequest(
    NotificationType Type,
    string RecipientEmail,
    string RecipientPhone,
    Dictionary<string, string> Variables,
    bool SendEmail = true,
    bool SendSms = false,
    string? CustomEmailTemplate = null,
    string? CustomSmsMessage = null);

/// <summary>
/// Notification result
/// </summary>
public record NotificationResult(
    bool EmailSent,
    bool SmsSent,
    string? EmailMessageId = null,
    string? SmsMessageId = null,
    string? EmailError = null,
    string? SmsError = null);

/// <summary>
/// Notification coordinator - orchestrates email + SMS sending
/// </summary>
public interface INotificationService
{
    Task<NotificationResult> SendNotificationAsync(NotificationRequest request);
    Task<NotificationResult> SendBookingConfirmationAsync(
        string recipientEmail,
        string recipientPhone,
        string bookingId,
        string guestName,
        string propertyName,
        string roomNumber,
        string checkInDate,
        string checkOutDate,
        decimal totalPrice,
        string currency,
        bool sendSms = false);
    Task<NotificationResult> SendCheckInReminderAsync(
        string recipientEmail,
        string recipientPhone,
        string guestName,
        string propertyName,
        string checkInTime,
        bool sendSms = true);
    Task<NotificationResult> SendPaymentConfirmationAsync(
        string recipientEmail,
        string recipientPhone,
        string bookingId,
        string transactionId,
        decimal amount,
        string currency,
        bool sendSms = false);
    Task<NotificationResult> SendCancellationAsync(
        string recipientEmail,
        string recipientPhone,
        string bookingId,
        string guestName,
        string cancellationReason,
        decimal refundAmount,
        string currency,
        bool sendSms = false);
    Task<NotificationResult> SendFeedbackRequestAsync(
        string recipientEmail,
        string recipientPhone,
        string guestName,
        string propertyName,
        string feedbackFormUrl,
        bool sendSms = false);
}

/// <summary>
/// Implementation of notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        ISmsService smsService,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send notification with custom template
    /// </summary>
    public async Task<NotificationResult> SendNotificationAsync(NotificationRequest request)
    {
        var emailResult = new EmailResult(false, "", "Not sent");
        var smsResult = new SmsResult(false, "", "Not sent");

        try
        {
            // Determine template based on type
            var (emailTemplate, defaultSmsMessage) = GetTemplateAndMessage(request.Type);
            var templateName = request.CustomEmailTemplate ?? emailTemplate;
            var smsMessage = request.CustomSmsMessage ?? defaultSmsMessage;

            // Send email
            if (request.SendEmail)
            {
                try
                {
                    emailResult = await _emailService.SendTemplateAsync(
                        templateName,
                        request.RecipientEmail,
                        request.Variables);

                    _logger.LogInformation(
                        "Email sent: Type={NotificationType} Email={Email} Template={Template}",
                        request.Type, request.RecipientEmail, templateName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email: {Email}", request.RecipientEmail);
                    emailResult = new EmailResult(false, "", ex.Message);
                }
            }

            // Send SMS
            if (request.SendSms && !string.IsNullOrEmpty(request.RecipientPhone))
            {
                try
                {
                    // Replace variables in SMS message
                    var processedSms = smsMessage;
                    foreach (var variable in request.Variables)
                    {
                        processedSms = processedSms.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                    }

                    smsResult = await _smsService.SendAsync(
                        new SmsRequest(request.RecipientPhone, processedSms));

                    _logger.LogInformation(
                        "SMS sent: Type={NotificationType} Phone={Phone}",
                        request.Type, MaskPhoneNumber(request.RecipientPhone));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SMS: {Phone}", MaskPhoneNumber(request.RecipientPhone));
                    smsResult = new SmsResult(false, "", ex.Message);
                }
            }

            return new NotificationResult(
                emailResult.IsSuccess,
                smsResult.IsSuccess,
                emailResult.MessageId,
                smsResult.MessageId,
                emailResult.IsSuccess ? null : emailResult.ErrorMessage,
                smsResult.IsSuccess ? null : smsResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendNotificationAsync");
            return new NotificationResult(false, false, null, null, ex.Message, ex.Message);
        }
    }

    /// <summary>
    /// Send booking confirmation (email + optional SMS)
    /// </summary>
    public async Task<NotificationResult> SendBookingConfirmationAsync(
        string recipientEmail,
        string recipientPhone,
        string bookingId,
        string guestName,
        string propertyName,
        string roomNumber,
        string checkInDate,
        string checkOutDate,
        decimal totalPrice,
        string currency,
        bool sendSms = false)
    {
        var nightCount = (DateTime.Parse(checkOutDate) - DateTime.Parse(checkInDate)).Days;

        var variables = new Dictionary<string, string>
        {
            { "BookingId", bookingId },
            { "GuestName", guestName },
            { "PropertyName", propertyName },
            { "RoomNumber", roomNumber },
            { "RoomType", "Standard" }, // Should come from room details
            { "CheckInDate", checkInDate },
            { "CheckOutDate", checkOutDate },
            { "CheckInTime", "14:00" }, // Should be configurable
            { "CheckOutTime", "11:00" },
            { "NightCount", nightCount.ToString() },
            { "TotalPrice", totalPrice.ToString("F2") },
            { "Currency", currency },
            { "BookingDetailsUrl", $"https://app.safaristackpms.com/bookings/{bookingId}" },
            { "PropertyEmail", "info@property.local" },
            { "PropertyPhone", "+27 123 456 7890" }
        };

        var request = new NotificationRequest(
            NotificationType.BookingConfirmation,
            recipientEmail,
            recipientPhone,
            variables,
            SendEmail: true,
            SendSms: sendSms);

        return await SendNotificationAsync(request);
    }

    /// <summary>
    /// Send check-in reminder (email + SMS)
    /// </summary>
    public async Task<NotificationResult> SendCheckInReminderAsync(
        string recipientEmail,
        string recipientPhone,
        string guestName,
        string propertyName,
        string checkInTime,
        bool sendSms = true)
    {
        var variables = new Dictionary<string, string>
        {
            { "GuestName", guestName },
            { "PropertyName", propertyName },
            { "CheckInTime", checkInTime },
            { "CheckInDate", DateTime.Today.ToString("yyyy-MM-dd") },
            { "RoomNumber", "TBD" },
            { "RoomType", "TBD" },
            { "CheckOutDate", DateTime.Today.AddDays(1).ToString("yyyy-MM-dd") },
            { "CheckOutTime", "11:00" },
            { "PropertyPhone", "+27 123 456 7890" },
            { "PropertyEmail", "info@property.local" },
            { "PropertyWebsite", "https://property.local" }
        };

        var request = new NotificationRequest(
            NotificationType.CheckInReminder,
            recipientEmail,
            recipientPhone,
            variables,
            SendEmail: true,
            SendSms: sendSms);

        return await SendNotificationAsync(request);
    }

    /// <summary>
    /// Send payment confirmation (email + optional SMS)
    /// </summary>
    public async Task<NotificationResult> SendPaymentConfirmationAsync(
        string recipientEmail,
        string recipientPhone,
        string bookingId,
        string transactionId,
        decimal amount,
        string currency,
        bool sendSms = false)
    {
        var variables = new Dictionary<string, string>
        {
            { "BookingId", bookingId },
            { "TransactionId", transactionId },
            { "PaymentAmount", amount.ToString("F2") },
            { "Currency", currency },
            { "PaymentMethod", "Card" }, // Should be dynamic
            { "PaymentDateTime", DateTime.UtcNow.ToString("G") },
            { "PropertyName", "Property" }
        };

        var request = new NotificationRequest(
            NotificationType.PaymentConfirmation,
            recipientEmail,
            recipientPhone,
            variables,
            SendEmail: true,
            SendSms: sendSms);

        return await SendNotificationAsync(request);
    }

    /// <summary>
    /// Send cancellation confirmation (email + optional SMS)
    /// </summary>
    public async Task<NotificationResult> SendCancellationAsync(
        string recipientEmail,
        string recipientPhone,
        string bookingId,
        string guestName,
        string cancellationReason,
        decimal refundAmount,
        string currency,
        bool sendSms = false)
    {
        var variables = new Dictionary<string, string>
        {
            { "BookingId", bookingId },
            { "GuestName", guestName },
            { "CancellationReason", cancellationReason },
            { "RefundAmount", refundAmount.ToString("F2") },
            { "Currency", currency },
            { "PropertyName", "Property" },
            { "PropertyEmail", "info@property.local" },
            { "PropertyPhone", "+27 123 456 7890" },
            { "SupportUrl", "https://support.safaristackpms.com" }
        };

        var request = new NotificationRequest(
            NotificationType.BookingCancellation,
            recipientEmail,
            recipientPhone,
            variables,
            SendEmail: true,
            SendSms: sendSms);

        return await SendNotificationAsync(request);
    }

    /// <summary>
    /// Send feedback request (email + optional SMS)
    /// </summary>
    public async Task<NotificationResult> SendFeedbackRequestAsync(
        string recipientEmail,
        string recipientPhone,
        string guestName,
        string propertyName,
        string feedbackFormUrl,
        bool sendSms = false)
    {
        var variables = new Dictionary<string, string>
        {
            { "GuestName", guestName },
            { "PropertyName", propertyName },
            { "FeedbackFormUrl", feedbackFormUrl },
            { "PropertyEmail", "info@property.local" },
            { "PropertyPhone", "+27 123 456 7890" }
        };

        var request = new NotificationRequest(
            NotificationType.FeedbackRequest,
            recipientEmail,
            recipientPhone,
            variables,
            SendEmail: true,
            SendSms: sendSms);

        return await SendNotificationAsync(request);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private (string EmailTemplate, string SmsMessage) GetTemplateAndMessage(NotificationType type)
    {
        return type switch
        {
            NotificationType.BookingConfirmation => (
                "booking_confirmation",
                "Booking confirmed at {{PropertyName}}! Booking ID: {{BookingId}}. Check-in: {{CheckInDate}}"),
            
            NotificationType.BookingCancellation => (
                "booking_cancellation",
                "Your booking {{BookingId}} has been cancelled. Refund: {{RefundAmount}} {{Currency}}"),
            
            NotificationType.CheckInReminder => (
                "check_in_reminder",
                "Reminder: Check-in at {{PropertyName}} today at {{CheckInTime}}. See you soon!"),
            
            NotificationType.PaymentConfirmation => (
                "payment_confirmation",
                "Payment received! {{PaymentAmount}} {{Currency}} for booking {{BookingId}}"),
            
            NotificationType.FeedbackRequest => (
                "feedback_request",
                "Thank you for staying at {{PropertyName}}! Please share your feedback."),
            
            _ => ("booking_confirmation", "Message from {{PropertyName}}")
        };
    }

    private string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "***";

        return "***" + phoneNumber.Substring(phoneNumber.Length - 4);
    }
}
