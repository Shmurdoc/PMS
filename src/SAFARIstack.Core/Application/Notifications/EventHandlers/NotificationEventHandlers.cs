using MediatR;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Application.Notifications.EventHandlers;

// ═══════════════════════════════════════════════════════════════════════
//  DOMAIN EVENT HANDLERS — Wire guest/staff activities to notifications
//  These handlers subscribe to domain events and queue email notifications
//  using the property's custom templates and preferences.
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// When a booking is created → send confirmation email to the guest.
/// </summary>
public class BookingCreatedNotificationHandler : INotificationHandler<BookingCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public BookingCreatedNotificationHandler(INotificationService notificationService, IUnitOfWork uow)
    {
        _notificationService = notificationService;
        _uow = uow;
    }

    public async Task Handle(BookingCreatedEvent notification, CancellationToken ct)
    {
        var booking = await _uow.Bookings.GetByIdAsync(notification.BookingId, ct);
        if (booking is null) return;

        var guest = await _uow.Guests.GetByIdAsync(booking.GuestId, ct);
        if (guest?.Email is null) return;

        await _notificationService.QueueNotificationAsync(
            booking.PropertyId,
            NotificationType.BookingConfirmation,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["BookingRef"] = notification.BookingReference,
                ["PropertyName"] = "Your Lodge",
                ["CheckInDate"] = booking.CheckInDate.ToString("dd MMM yyyy"),
                ["CheckOutDate"] = booking.CheckOutDate.ToString("dd MMM yyyy"),
                ["Amount"] = booking.TotalAmount.ToString("N2"),
                ["Currency"] = "ZAR",
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);
    }
}

/// <summary>
/// When a booking is cancelled → notify the guest.
/// </summary>
public class BookingCancelledNotificationHandler : INotificationHandler<BookingCancelledEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public BookingCancelledNotificationHandler(INotificationService notificationService, IUnitOfWork uow)
    {
        _notificationService = notificationService;
        _uow = uow;
    }

    public async Task Handle(BookingCancelledEvent notification, CancellationToken ct)
    {
        var booking = await _uow.Bookings.GetByIdAsync(notification.BookingId, ct);
        if (booking is null) return;

        var guest = await _uow.Guests.GetByIdAsync(booking.GuestId, ct);
        if (guest?.Email is null) return;

        await _notificationService.QueueNotificationAsync(
            booking.PropertyId,
            NotificationType.BookingCancellation,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["BookingRef"] = notification.BookingReference,
                ["PropertyName"] = "Your Lodge",
                ["Reason"] = notification.Reason ?? "Not specified",
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);
    }
}

/// <summary>
/// When a guest checks in → send a welcome email.
/// </summary>
public class BookingCheckedInNotificationHandler : INotificationHandler<BookingCheckedInEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public BookingCheckedInNotificationHandler(INotificationService notificationService, IUnitOfWork uow)
    {
        _notificationService = notificationService;
        _uow = uow;
    }

    public async Task Handle(BookingCheckedInEvent notification, CancellationToken ct)
    {
        var booking = await _uow.Bookings.GetByIdAsync(notification.BookingId, ct);
        if (booking is null) return;

        var guest = await _uow.Guests.GetByIdAsync(booking.GuestId, ct);
        if (guest?.Email is null) return;

        await _notificationService.QueueNotificationAsync(
            booking.PropertyId,
            NotificationType.CheckInReminder,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["BookingRef"] = notification.BookingReference,
                ["PropertyName"] = "Your Lodge",
                ["CheckInDate"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);
    }
}

/// <summary>
/// When a guest checks out → send a thank-you email (and optionally a review request).
/// </summary>
public class BookingCheckedOutNotificationHandler : INotificationHandler<BookingCheckedOutEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public BookingCheckedOutNotificationHandler(INotificationService notificationService, IUnitOfWork uow)
    {
        _notificationService = notificationService;
        _uow = uow;
    }

    public async Task Handle(BookingCheckedOutEvent notification, CancellationToken ct)
    {
        var booking = await _uow.Bookings.GetByIdAsync(notification.BookingId, ct);
        if (booking is null) return;

        var guest = await _uow.Guests.GetByIdAsync(booking.GuestId, ct);
        if (guest?.Email is null) return;

        // Send check-out confirmation
        await _notificationService.QueueNotificationAsync(
            booking.PropertyId,
            NotificationType.CheckOutReminder,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["BookingRef"] = notification.BookingReference,
                ["PropertyName"] = "Your Lodge",
                ["CheckOutTime"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);

        // Also send review request if enabled
        await _notificationService.QueueNotificationAsync(
            booking.PropertyId,
            NotificationType.ReviewRequest,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["PropertyName"] = "Your Lodge",
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);
    }
}

/// <summary>
/// When a payment is recorded → send receipt to the guest.
/// </summary>
public class PaymentRecordedNotificationHandler : INotificationHandler<PaymentRecordedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public PaymentRecordedNotificationHandler(INotificationService notificationService, IUnitOfWork uow)
    {
        _notificationService = notificationService;
        _uow = uow;
    }

    public async Task Handle(PaymentRecordedEvent notification, CancellationToken ct)
    {
        // Look up the folio to find the guest
        var folio = await _uow.Folios.GetByIdAsync(notification.FolioId, ct);
        if (folio is null) return;

        var guest = await _uow.Guests.GetByIdAsync(folio.GuestId, ct);
        if (guest?.Email is null) return;

        await _notificationService.QueueNotificationAsync(
            folio.PropertyId,
            NotificationType.PaymentReceipt,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["PaymentRef"] = notification.PaymentId.ToString()[..8],
                ["Amount"] = notification.Amount.ToString("N2"),
                ["Currency"] = "ZAR",
                ["PaymentMethod"] = notification.Method.ToString(),
                ["PropertyName"] = "Your Lodge",
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);
    }
}

/// <summary>
/// When an invoice is finalized → send it to the guest.
/// </summary>
public class InvoiceFinalizedNotificationHandler : INotificationHandler<InvoiceFinalizedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public InvoiceFinalizedNotificationHandler(INotificationService notificationService, IUnitOfWork uow)
    {
        _notificationService = notificationService;
        _uow = uow;
    }

    public async Task Handle(InvoiceFinalizedEvent notification, CancellationToken ct)
    {
        var invoice = await _uow.Invoices.GetByIdAsync(notification.InvoiceId, ct);
        if (invoice is null) return;

        var guest = await _uow.Guests.GetByIdAsync(invoice.GuestId, ct);
        if (guest?.Email is null) return;

        await _notificationService.QueueNotificationAsync(
            invoice.PropertyId,
            NotificationType.InvoiceSent,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["InvoiceNumber"] = notification.InvoiceNumber,
                ["Amount"] = invoice.TotalAmount.ToString("N2"),
                ["Currency"] = "ZAR",
                ["DueDate"] = invoice.DueDate.ToString("dd MMM yyyy"),
                ["PropertyName"] = "Your Lodge",
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);
    }
}

/// <summary>
/// When a new guest profile is created → send a welcome email.
/// </summary>
public class GuestCreatedNotificationHandler : INotificationHandler<GuestCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _uow;

    public GuestCreatedNotificationHandler(INotificationService notificationService, IUnitOfWork uow)
    {
        _notificationService = notificationService;
        _uow = uow;
    }

    public async Task Handle(GuestCreatedEvent notification, CancellationToken ct)
    {
        var guest = await _uow.Guests.GetByIdAsync(notification.GuestId, ct);
        if (guest?.Email is null) return;

        await _notificationService.QueueNotificationAsync(
            notification.PropertyId,
            NotificationType.SystemAlert,
            NotificationChannel.Email,
            guest.Email,
            new Dictionary<string, string>
            {
                ["GuestName"] = guest.FullName,
                ["PropertyName"] = "Your Lodge",
                ["AlertMessage"] = $"Welcome to our lodge, {guest.FullName}! Your guest profile has been created.",
                ["Date"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
            },
            guestId: guest.Id,
            ct: ct);
    }
}
