# Email & SMS Notification Services Integration Guide

## Overview

The SAFARIstack PMS now includes comprehensive email and SMS notification services:

- **IEmailService** - SMTP-based email with HTML templates
- **ISmsService** - Multi-provider SMS (Twilio, AWS SNS, local gateway, generic API)
- **INotificationService** - Coordinator that sends both email and SMS

All services include error handling, logging, and are designed to fail gracefully if not configured.

## Configuration

### appsettings.json Setup

```json
{
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "${SMTP_USERNAME}",
    "SmtpPassword": "${SMTP_PASSWORD}",
    "FromEmail": "noreply@safaristackpms.com",
    "FromDisplayName": "SAFARIstack PMS",
    "EnableSsl": true
  },
  "Sms": {
    "Provider": "local-gateway",
    "Enabled": true,
    "ApiKey": "${SMS_API_KEY}",
    "ApiSecret": "${SMS_API_SECRET}",
    "SenderId": "SAFARIstack",
    "ApiEndpoint": "https://sms-gateway.local/api/send"
  }
}
```

### Environment Variables

**Email (SMTP):**
- `SMTP_USERNAME` - Email account username
- `SMTP_PASSWORD` - Email account password (use app password for Gmail 2FA)

**SMS:**
- `SMS_API_KEY` - API key from SMS provider
- `SMS_API_SECRET` - API secret/auth token
- `SMS_GATEWAY_ENDPOINT` - REST endpoint for local gateway

## Service Registration

Already registered in `Program.cs` (lines ~244-266):

```csharp
// Email configuration
builder.Services.Configure<EmailConfiguration>(
    builder.Configuration.GetSection("Email"));

// Email service (SMTP)
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// SMS configuration  
builder.Services.Configure<SmsConfiguration>(
    builder.Configuration.GetSection("Sms"));

// SMS service with HttpClient
builder.Services.AddHttpClient<ISmsService, SmsService>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));

// Notification coordinator
builder.Services.AddScoped<INotificationService, NotificationService>();
```

## Usage in Endpoints

### Inject the Service

```csharp
app.MapPost("/api/bookings", async (
    CreateBookingRequest req,
    ApplicationDbContext db,
    INotificationService notificationService,  // ← Inject
    CancellationToken ct) =>
{
    // Create booking
    var booking = Booking.Create(req...);
    db.Bookings.Add(booking);
    await db.SaveChangesAsync(ct);

    // Get guest and room details
    var guest = booking.Guest;
    var room = booking.Room;

    // Send confirmation email + SMS
    await notificationService.SendBookingConfirmationAsync(
        recipientEmail: guest.Email,
        recipientPhone: guest.PhoneNumber,
        bookingId: booking.Id.ToString(),
        guestName: guest.FullName,
        propertyName: "Your Property Name",
        roomNumber: room.RoomNumber.ToString(),
        checkInDate: booking.CheckInDate.ToString("yyyy-MM-dd"),
        checkOutDate: booking.CheckOutDate.ToString("yyyy-MM-dd"),
        totalPrice: booking.TotalPrice,
        currency: "ZAR",
        sendSms: true);  // Set to true to also send SMS

    return Results.Created($"/api/bookings/{booking.Id}", booking);
});
```

## Available Methods

### INotificationService

**Specialized Methods (Recommended):**

```csharp
// Booking confirmation
await notificationService.SendBookingConfirmationAsync(
    recipientEmail: "guest@example.com",
    recipientPhone: "+27123456789",
    bookingId: "BOOK-001",
    guestName: "John Doe",
    propertyName: "My Hotel",
    roomNumber: "101",
    checkInDate: "2024-03-20",
    checkOutDate: "2024-03-25",
    totalPrice: 5000m,
    currency: "ZAR",
    sendSms: false);

// Check-in reminder (24 hours before)
await notificationService.SendCheckInReminderAsync(
    recipientEmail: "guest@example.com",
    recipientPhone: "+27123456789",
    guestName: "John Doe",
    propertyName: "My Hotel",
    checkInTime: "14:00",
    sendSms: true);

// Payment confirmation
await notificationService.SendPaymentConfirmationAsync(
    recipientEmail: "guest@example.com",
    recipientPhone: "+27123456789",
    bookingId: "BOOK-001",
    transactionId: "TXN-12345",
    amount: 5000m,
    currency: "ZAR",
    sendSms: false);

// Booking cancellation
await notificationService.SendCancellationAsync(
    recipientEmail: "guest@example.com",
    recipientPhone: "+27123456789",
    bookingId: "BOOK-001",
    guestName: "John Doe",
    cancellationReason: "Guest requested",
    refundAmount: 4500m,
    currency: "ZAR",
    sendSms: true);

// Feedback request
await notificationService.SendFeedbackRequestAsync(
    recipientEmail: "guest@example.com",
    recipientPhone: "+27123456789",
    guestName: "John Doe",
    propertyName: "My Hotel",
    feedbackFormUrl: "https://feedback.safaristackpms.com/form/123",
    sendSms: false);
```

**Generic Method (Custom Templates):**

```csharp
var result = await notificationService.SendNotificationAsync(
    new NotificationRequest(
        Type: NotificationType.BookingConfirmation,
        RecipientEmail: "guest@example.com",
        RecipientPhone: "+27123456789",
        Variables: new Dictionary<string, string>
        {
            { "GuestName", "John Doe" },
            { "PropertyName", "My Hotel" },
            { "BookingId", "BOOK-001" },
            // ... more variables
        },
        SendEmail: true,
        SendSms: true));
```

### IEmailService (Low-level)

```csharp
// Send template-based email
var result = await emailService.SendTemplateAsync(
    templateName: "booking_confirmation",
    toEmail: "guest@example.com",
    variables: new Dictionary<string, string>
    {
        { "GuestName", "John Doe" },
        { "PropertyName", "My Hotel" },
        // ... more variables
    });

// Send custom HTML email
var result = await emailService.SendAsync(
    new EmailRequest(
        ToEmail: "guest@example.com",
        Subject: "Your Booking Confirmation",
        HtmlBody: "<h1>Thank you!</h1>",
        BccEmails: new List<string> { "archive@safaristackpms.com" }));

// Check available templates
var templates = await emailService.GetAvailableTemplatesAsync();
// ["booking_confirmation", "check_in_reminder", "cancellation_confirmation", ...]

// Check if configured
var isConfigured = await emailService.IsConfiguredAsync();
```

### ISmsService (Low-level)

```csharp
// Send SMS
var result = await smsService.SendAsync(
    new SmsRequest(
        PhoneNumber: "+27123456789",
        Message: "Your booking is confirmed!"));

// Send verification code
var result = await smsService.SendVerificationCodeAsync(
    phoneNumber: "+27123456789",
    code: "123456");

// Send booking notification
var result = await smsService.SendBookingNotificationAsync(
    phoneNumber: "+27123456789",
    bookingId: "BOOK-001",
    propertyName: "My Hotel",
    checkInDate: "2024-03-20");

// Send check-in reminder
var result = await smsService.SendCheckInReminderAsync(
    phoneNumber: "+27123456789",
    guestName: "John Doe",
    propertyName: "My Hotel",
    checkInTime: "14:00");

// Bulk SMS sending
var results = await smsService.SendBulkAsync(
    new List<SmsRequest>
    {
        new SmsRequest("+27123456789", "Message 1"),
        new SmsRequest("+27987654321", "Message 2"),
        // ...
    });
```

## Available Email Templates

The system comes with 6 built-in templates:

| Template Name | Purpose | Variables |
|---|---|---|
| `booking_confirmation` | Confirm booking made by guest | BookingId, GuestName, PropertyName, RoomNumber, CheckInDate, CheckOutDate, TotalPrice, Currency |
| `check_in_reminder` | Remind guest day of check-in | GuestName, PropertyName, CheckInTime, CheckInDate, RoomNumber |
| `booking_cancellation` | Confirm cancellation & refund | BookingId, GuestName, RefundAmount, Currency, CancellationReason |
| `payment_confirmation` | Confirm payment received | TransactionId, PaymentAmount, Currency, PaymentDateTime |
| `feedback_request` | Request post-stay feedback | GuestName, PropertyName, FeedbackFormUrl |
| `account_password_reset` | Password reset link | ResetPasswordUrl |

## SMS Providers

### 1. Local Gateway (Default)
**Configuration:**
```json
{
  "Sms": {
    "Provider": "local-gateway",
    "Enabled": true,
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "ApiEndpoint": "https://sms-gateway.local/api/send"
  }
}
```

**Expected Endpoint Behavior:**
- POST to ApiEndpoint with JSON payload
- Authorization: X-API-Key header
- Returns: `{ "messageId": "...", "status": "sent" }`

### 2. Twilio
**Configuration:**
```json
{
  "Sms": {
    "Provider": "twilio",
    "Enabled": true,
    "ApiKey": "${TWILIO_ACCOUNT_SID}",
    "ApiSecret": "${TWILIO_AUTH_TOKEN}",
    "SenderId": "+12125551234"
  }
}
```

### 3. AWS SNS (Backend Processing)
```json
{
  "Sms": {
    "Provider": "aws-sns",
    "Enabled": true,
    "ApiKey": "${AWS_ACCESS_KEY_ID}",
    "ApiSecret": "${AWS_SECRET_ACCESS_KEY}",
    "SenderId": "SAFARIstack"
  }
}
```

### 4. Generic API
For any other SMS provider with REST API:
```json
{
  "Sms": {
    "Provider": "generic-api",
    "Enabled": true,
    "ApiKey": "your-api-key",
    "ApiEndpoint": "https://provider.com/api/messages",
    "SenderId": "YourSender"
  }
}
```

## Error Handling

All services fail gracefully and log errors:

```csharp
var result = await notificationService.SendBookingConfirmationAsync(...);

if (!result.EmailSent)
{
    _logger.LogWarning("Email failed: {Error}", result.EmailError);
    // Continue without throwing - SMS might succeed
}

if (!result.SmsSent)
{
    _logger.LogWarning("SMS failed: {Error}", result.SmsError);
}

// At least one succeeded?
if (result.EmailSent || result.SmsSent)
{
    // Notification partially succeeded
}
```

## Best Practices

### 1. **Configuration**
- Never hardcode credentials - use environment variables
- Keep ApiSecret secure (don't log it)
- Test with dummy phone numbers first

### 2. **Usage**
- Always supply both email and phone, but control `sendSms` flag
- Validate phone numbers before sending
- Don't send SMS without consent (check guest preferences)

### 3. **Performance**
- SMS service has 10-second timeout per message
- Bulk SMS works well up to ~100 messages
- Consider background queues (Hangfire) for high volume

### 4. **Testing**
- Email: Use `mailhog` or similar local SMTP for development
- SMS: Use provider sandbox mode or dummy numbers
- Check logs for errors - all operations are logged

## Integration Checklist

- [x] EmailService created with 6 built-in templates
- [x] SmsService created with multi-provider support
- [x] NotificationService created as coordinator
- [x] Program.cs registrations added
- [ ] Integrate SendBookingConfirmationAsync into POST /api/bookings
- [ ] Integrate SendCheckInReminderAsync into background service (24h before check-in)
- [ ] Integrate SendPaymentConfirmationAsync into payment processing
- [ ] Integrate SendCancellationAsync into booking cancellation
- [ ] Integrate SendFeedbackRequestAsync into check-out workflow
- [ ] Add SMS consent flag to Guest model
- [ ] Create notification preferences UI
- [ ] Monitor notification delivery success rates

## Example: Complete Booking Endpoint Integration

```csharp
app.MapPost("/api/bookings", async (
    CreateBookingRequest req,
    ApplicationDbContext db,
    INotificationService notificationService,
    ISignalRNotificationService signalRService,
    CancellationToken ct) =>
{
    // Validate
    var guest = await db.Guests.FindAsync(new object[] { req.GuestId }, ct);
    if (guest is null)
        return Results.NotFound("Guest not found");

    var room = await db.Rooms.FindAsync(new object[] { req.RoomId }, ct);
    if (room is null)
        return Results.NotFound("Room not found");

    // Create booking
    var booking = Booking.Create(
        req.PropertyId, req.GuestId, req.RoomId,
        req.CheckInDate, req.CheckOutDate, req.Notes ?? "");

    db.Bookings.Add(booking);
    await db.SaveChangesAsync(ct);

    // 1. Send email confirmation + SMS (if guest opted in)
    var notificationResult = await notificationService.SendBookingConfirmationAsync(
        recipientEmail: guest.Email,
        recipientPhone: guest.PhoneNumber,
        bookingId: booking.Id.ToString(),
        guestName: guest.FullName,
        propertyName: req.PropertyName,
        roomNumber: room.RoomNumber.ToString(),
        checkInDate: booking.CheckInDate.ToString("yyyy-MM-dd"),
        checkOutDate: booking.CheckOutDate.ToString("yyyy-MM-dd"),
        totalPrice: booking.TotalPrice,
        currency: "ZAR",
        sendSms: guest.HasOptedInToSms);

    if (!notificationResult.EmailSent)
    {
        _logger.LogWarning("Failed to send booking confirmation email to {Email}", guest.Email);
    }

    // 2. Broadcast real-time update to property
    await signalRService.BroadcastBookingCreateAsync(
        propertyId: req.PropertyId,
        bookingId: booking.Id,
        guestName: guest.FullName,
        roomNumber: room.RoomNumber,
        totalPrice: booking.TotalPrice,
        checkIn: booking.CheckInDate,
        checkOut: booking.CheckOutDate);

    return Results.Created($"/api/bookings/{booking.Id}", new
    {
        booking.Id,
        booking.PropertyId,
        booking.CheckInDate,
        booking.CheckOutDate,
        GuestName = guest.FullName,
        RoomNumber = room.RoomNumber,
        booking.TotalPrice,
        ConfirmationEmailSent = notificationResult.EmailSent,
        ConfirmationSmsSent = notificationResult.SmsSent
    });
})
.WithName("CreateBooking")
.WithOpenApi();
```

## Testing Locally

### Email (Using Mailhog)

1. Run Mailhog locally: `mailhog`
2. Configure appsettings.json:
```json
{
  "Email": {
    "SmtpServer": "localhost",
    "SmtpPort": 1025,
    "SmtpUsername": "test",
    "SmtpPassword": "test",
    "FromEmail": "noreply@test.local",
    "EnableSsl": false
  }
}
```
3. View emails at http://localhost:8025

### SMS (Using Mock Provider)

1. Configure appsettings.json:
```json
{
  "Sms": {
    "Provider": "local-gateway",
    "Enabled": true,
    "ApiKey": "test-key",
    "ApiSecret": "test-secret",
    "ApiEndpoint": "https://httpbin.org/post"  // Echo service
  }
}
```
2. Check logs - SMS "delivery" logged to console
3. Integrate with Twilio test numbers in production

## Troubleshooting

| Issue | Solution |
|---|---|
| "Email service not configured" | Verify SMTP credentials in appsettings.json |
| "Email sent but not received" | Check spam folder, verify From address, test with app password for Gmail |
| SMTP timeout | Increase SmtpPort to 587 or 25, check firewall, verify EnableSsl setting |
| SMS not sending | Enable SMS in config, verify api endpoint is reachable, check API key/secret |
| Tests failing | Use Mailhog for email, mock HTTP client for SMS in unit tests |

