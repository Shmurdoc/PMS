using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SAFARIstack.Infrastructure.Services.Notifications;

/// <summary>
/// Email configuration (injected from appsettings.json)
/// </summary>
public class EmailConfiguration
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromDisplayName { get; set; } = "SAFARIstack PMS";
    public bool EnableSsl { get; set; } = true;
}

/// <summary>
/// Email request DTO
/// </summary>
public record EmailRequest(
    string ToEmail,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null,
    Dictionary<string, string>? Headers = null,
    List<string>? BccEmails = null);

/// <summary>
/// Email send result
/// </summary>
public record EmailResult(
    bool IsSuccess,
    string MessageId,
    string? ErrorMessage = null);

/// <summary>
/// Email service interface
/// </summary>
public interface IEmailService
{
    Task<EmailResult> SendAsync(EmailRequest request);
    Task<EmailResult> SendTemplateAsync(string templateName, string toEmail, Dictionary<string, string> variables);
    Task<bool> IsConfiguredAsync();
    Task<List<string>> GetAvailableTemplatesAsync();
}

/// <summary>
/// Implementation using SMTP (Gmail, Office365, or custom SMTP server)
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly Dictionary<string, string> _templates;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _config = configuration.GetSection("Email").Get<EmailConfiguration>() ?? new EmailConfiguration();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templates = InitializeTemplates();
    }

    /// <summary>
    /// Send email using SMTP
    /// </summary>
    public async Task<EmailResult> SendAsync(EmailRequest request)
    {
        try
        {
            if (!await IsConfiguredAsync())
            {
                _logger.LogWarning("Email service not configured. Skipping email send to {ToEmail}", request.ToEmail);
                return new EmailResult(false, "", "Email service not configured");
            }

            using (var client = new SmtpClient(_config.SmtpServer, _config.SmtpPort))
            {
                client.EnableSsl = _config.EnableSsl;
                client.Credentials = new NetworkCredential(_config.SmtpUsername, _config.SmtpPassword);
                client.Timeout = 10000;

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(_config.FromEmail, _config.FromDisplayName);
                    mailMessage.To.Add(new MailAddress(request.ToEmail));

                    if (request.BccEmails?.Count > 0)
                    {
                        foreach (var bcc in request.BccEmails)
                        {
                            mailMessage.Bcc.Add(new MailAddress(bcc));
                        }
                    }

                    mailMessage.Subject = request.Subject;
                    mailMessage.Body = request.HtmlBody;
                    mailMessage.IsBodyHtml = true;

                    // Add custom headers
                    if (request.Headers?.Count > 0)
                    {
                        foreach (var header in request.Headers)
                        {
                            try
                            {
                                mailMessage.Headers.Add(header.Key, header.Value);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to add header {HeaderKey}", header.Key);
                            }
                        }
                    }

                    // Add alternative plain text version
                    if (!string.IsNullOrEmpty(request.PlainTextBody))
                    {
                        var plainTextView = AlternateView.CreateAlternateViewFromString(
                            request.PlainTextBody,
                            Encoding.UTF8,
                            "text/plain");
                        mailMessage.AlternateViews.Add(plainTextView);
                    }

                    await client.SendMailAsync(mailMessage);

                    _logger.LogInformation(
                        "Email sent successfully to {ToEmail} with subject '{Subject}'",
                        request.ToEmail, request.Subject);

                    return new EmailResult(true, Guid.NewGuid().ToString());
                }
            }
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {ToEmail}", request.ToEmail);
            return new EmailResult(false, "", $"SMTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {ToEmail}", request.ToEmail);
            return new EmailResult(false, "", ex.Message);
        }
    }

    /// <summary>
    /// Send email using template with variable substitution
    /// </summary>
    public async Task<EmailResult> SendTemplateAsync(
        string templateName,
        string toEmail,
        Dictionary<string, string> variables)
    {
        try
        {
            if (!_templates.TryGetValue(templateName, out var template))
            {
                _logger.LogWarning("Email template '{TemplateName}' not found", templateName);
                return new EmailResult(false, "", $"Template '{templateName}' not found");
            }

            var subject = template.Split("---")[0].Trim();
            var htmlBody = template.Split("---")[1].Trim();

            // Replace variables
            foreach (var variable in variables)
            {
                htmlBody = htmlBody.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                subject = subject.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            var request = new EmailRequest(toEmail, subject, htmlBody);
            return await SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template email '{TemplateName}' to {ToEmail}", templateName, toEmail);
            return new EmailResult(false, "", ex.Message);
        }
    }

    /// <summary>
    /// Check if email service is configured
    /// </summary>
    public Task<bool> IsConfiguredAsync()
    {
        var isConfigured = !string.IsNullOrEmpty(_config.SmtpUsername)
            && !string.IsNullOrEmpty(_config.SmtpPassword)
            && !string.IsNullOrEmpty(_config.FromEmail);

        return Task.FromResult(isConfigured);
    }

    /// <summary>
    /// Get list of available email templates
    /// </summary>
    public Task<List<string>> GetAvailableTemplatesAsync()
    {
        return Task.FromResult(new List<string>(_templates.Keys));
    }

    /// <summary>
    /// Initialize email templates
    /// Format: "Subject\n---\nHTML Body"
    /// </summary>
    private Dictionary<string, string> InitializeTemplates()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ═══════════════════════════════════════════════════════════════
            //  BOOKING CONFIRMATION
            // ═══════════════════════════════════════════════════════════════
            {
                "booking_confirmation",
                """
Booking Confirmation - {{BookingId}} - {{PropertyName}}
---
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px; }
        .section { margin-top: 20px; padding: 15px; border-left: 4px solid #667eea; background: #f9f9f9; }
        .booking-details table { width: 100%; border-collapse: collapse; }
        .booking-details td { padding: 10px; border-bottom: 1px solid #ddd; }
        .booking-details .label { font-weight: bold; color: #667eea; }
        .button { display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin-top: 15px; }
        .footer { text-align: center; margin-top: 30px; font-size: 12px; color: #999; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Booking Confirmation ✓</h1>
            <p>Thank you for booking with {{PropertyName}}!</p>
        </div>

        <div class="section">
            <h3>Booking Details</h3>
            <table class="booking-details">
                <tr>
                    <td class="label">Booking ID:</td>
                    <td>{{BookingId}}</td>
                </tr>
                <tr>
                    <td class="label">Guest Name:</td>
                    <td>{{GuestName}}</td>
                </tr>
                <tr>
                    <td class="label">Room:</td>
                    <td>{{RoomNumber}} ({{RoomType}})</td>
                </tr>
                <tr>
                    <td class="label">Check-In:</td>
                    <td>{{CheckInDate}}</td>
                </tr>
                <tr>
                    <td class="label">Check-Out:</td>
                    <td>{{CheckOutDate}}</td>
                </tr>
                <tr>
                    <td class="label">Duration:</td>
                    <td>{{NightCount}} nights</td>
                </tr>
                <tr>
                    <td class="label">Total Price:</td>
                    <td><strong>{{TotalPrice}} {{Currency}}</strong></td>
                </tr>
            </table>
        </div>

        <div class="section">
            <h3>Important Information</h3>
            <ul>
                <li>Check-in time: {{CheckInTime}} | Check-out time: {{CheckOutTime}}</li>
                <li>Please bring your booking confirmation and valid ID</li>
                <li>In case of cancellation, refer to your cancellation policy</li>
            </ul>
        </div>

        <a href="{{BookingDetailsUrl}}" class="button">View Booking Details</a>

        <div class="footer">
            <p>If you did not make this booking, please contact {{PropertyEmail}} immediately</p>
            <p>&copy; {{PropertyName}} - All rights reserved</p>
        </div>
    </div>
</body>
</html>
"""
            },

            // ═══════════════════════════════════════════════════════════════
            //  CHECK-IN REMINDER
            // ═══════════════════════════════════════════════════════════════
            {
                "check_in_reminder",
                """
Reminder: Check-in {{CheckInTime}} - {{PropertyName}}
---
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .alert { background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; border-radius: 5px; }
        .button { display: inline-block; background: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin-top: 15px; }
    </style>
</head>
<body>
    <div class="container">
        <h2>Your Check-in is Today! 🎉</h2>
        <div class="alert">
            <strong>{{GuestName}}</strong>, your stay at <strong>{{PropertyName}}</strong> will begin today at {{CheckInTime}}
        </div>
        <p>
            <strong>Room:</strong> {{RoomNumber}} ({{RoomType}})<br>
            <strong>Check-out:</strong> {{CheckOutDate}} at {{CheckOutTime}}
        </p>
        <p>
            We're excited to welcome you! Have any questions? Contact us at {{PropertyPhone}} or {{PropertyEmail}}
        </p>
        <a href="{{PropertyWebsite}}" class="button">Visit Property Website</a>
    </div>
</body>
</html>
"""
            },

            // ═══════════════════════════════════════════════════════════════
            //  CANCELLATION CONFIRMATION
            // ═══════════════════════════════════════════════════════════════
            {
                "booking_cancellation",
                """
Booking Cancelled - {{BookingId}} - {{PropertyName}}
---
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #f5f5f5; padding: 20px; border-radius: 5px; }
        .refund-info { background: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin-top: 20px; border-radius: 5px; }
        .button { display: inline-block; background: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin-top: 15px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h2>Booking Cancelled</h2>
            <p>Your reservation at {{PropertyName}} has been cancelled.</p>
        </div>

        <table style="width: 100%; margin-top: 20px; border-collapse: collapse;">
            <tr style="background: #f9f9f9;">
                <td style="padding: 10px; font-weight: bold;">Booking ID:</td>
                <td style="padding: 10px;">{{BookingId}}</td>
            </tr>
            <tr>
                <td style="padding: 10px; font-weight: bold;">Guest Name:</td>
                <td style="padding: 10px;">{{GuestName}}</td>
            </tr>
            <tr style="background: #f9f9f9;">
                <td style="padding: 10px; font-weight: bold;">Original Check-in:</td>
                <td style="padding: 10px;">{{CheckInDate}}</td>
            </tr>
            <tr>
                <td style="padding: 10px; font-weight: bold;">Cancellation Reason:</td>
                <td style="padding: 10px;">{{CancellationReason}}</td>
            </tr>
        </table>

        <div class="refund-info">
            <strong>Refund Amount:</strong> {{RefundAmount}} {{Currency}}<br>
            <strong>Refund Status:</strong> Processing (expect within 5-7 business days)<br>
            <strong>Refund Method:</strong> Original payment method
        </div>

        <p style="margin-top: 20px;">
            If you have any questions about your cancellation or refund, please contact us at {{PropertyEmail}} or {{PropertyPhone}}.
        </p>

        <a href="{{SupportUrl}}" class="button">Contact Support</a>
    </div>
</body>
</html>
"""
            },

            // ═══════════════════════════════════════════════════════════════
            //  PAYMENT CONFIRMATION
            // ═══════════════════════════════════════════════════════════════
            {
                "payment_confirmation",
                """
Payment Received - {{BookingId}} - {{PropertyName}}
---
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .success { background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 20px; border-radius: 5px; }
        .payment-details { margin-top: 20px; padding: 15px; background: #f9f9f9; border-left: 4px solid #28a745; }
        table { width: 100%; border-collapse: collapse; }
        td { padding: 8px; border-bottom: 1px solid #ddd; }
        .label { font-weight: bold; }
    </style>
</head>
<body>
    <div class="container">
        <div class="success">
            <h2>Payment Received ✓</h2>
            <p>Thank you for your payment. Your booking is now confirmed.</p>
        </div>

        <div class="payment-details">
            <h3>Payment Details</h3>
            <table>
                <tr style="background: #f9f9f9;">
                    <td class="label">Transaction ID:</td>
                    <td>{{TransactionId}}</td>
                </tr>
                <tr>
                    <td class="label">Amount:</td>
                    <td><strong>{{PaymentAmount}} {{Currency}}</strong></td>
                </tr>
                <tr style="background: #f9f9f9;">
                    <td class="label">Payment Method:</td>
                    <td>{{PaymentMethod}}</td>
                </tr>
                <tr>
                    <td class="label">Date & Time:</td>
                    <td>{{PaymentDateTime}}</td>
                </tr>
                <tr style="background: #f9f9f9;">
                    <td class="label">Status:</td>
                    <td>Completed</td>
                </tr>
            </table>
        </div>

        <p style="margin-top: 20px;">
            Your booking is now confirmed. Keep this email as your receipt.
        </p>
    </div>
</body>
</html>
"""
            },

            // ═══════════════════════════════════════════════════════════════
            //  FEEDBACK REQUEST
            // ═══════════════════════════════════════════════════════════════
            {
                "feedback_request",
                """
We'd love your feedback! Rate your stay at {{PropertyName}}
---
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px; }
        .button { display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 10px 5px 10px 0; }
        .footer { text-align: center; margin-top: 30px; font-size: 12px; color: #999; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h2>Thank You for Staying with Us! 😊</h2>
            <p>{{GuestName}}, we'd love to hear about your experience at {{PropertyName}}</p>
        </div>

        <p style="margin-top: 20px;">
            Your feedback helps us improve our service and ensure every guest has a wonderful stay. 
            Please take a moment to rate your experience.
        </p>

        <p style="text-align: center; margin-top: 30px;">
            <a href="{{FeedbackFormUrl}}" class="button" style="background: #28a745;">Leave Feedback</a>
        </p>

        <p style="margin-top: 30px;">
            <strong>Questions or concerns?</strong><br>
            Contact us directly at {{PropertyEmail}} or call {{PropertyPhone}}
        </p>

        <div class="footer">
            <p>&copy; {{PropertyName}} - We appreciate your business!</p>
        </div>
    </div>
</body>
</html>
"""
            },

            // ═══════════════════════════════════════════════════════════════
            //  PASSWORD RESET
            // ═══════════════════════════════════════════════════════════════
            {
                "account_password_reset",
                """
Password Reset Request - {{PropertyName}}
---
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .alert { background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; border-radius: 5px; margin-bottom: 20px; }
        .button { display: inline-block; background: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin-top: 15px; }
        .expiry { color: #e74c3c; font-weight: bold; }
    </style>
</head>
<body>
    <div class="container">
        <h2>Password Reset Request</h2>
        
        <div class="alert">
            You requested a password reset for your {{PropertyName}} account.
        </div>

        <p>
            Click the button below to reset your password. This link is valid for <span class="expiry">24 hours</span>.
        </p>

        <a href="{{ResetPasswordUrl}}" class="button">Reset Password</a>

        <p style="margin-top: 30px; color: #999; font-size: 12px;">
            If you didn't request this, please ignore this email or contact support immediately.<br>
            Do not share this link with anyone.
        </p>
    </div>
</body>
</html>
"""
            }
        };
    }
}
