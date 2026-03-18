using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
//  GUEST PREFERENCE — Room & service preferences per guest
// ═══════════════════════════════════════════════════════════════════════
public class GuestPreference : Entity
{
    public Guid GuestId { get; private set; }
    public PreferenceCategory Category { get; private set; }
    public string Key { get; private set; } = string.Empty;        // "room_floor", "pillow_type"
    public string Value { get; private set; } = string.Empty;      // "high", "memory_foam"
    public string? Notes { get; private set; }

    public Guest Guest { get; private set; } = null!;

    private GuestPreference() { }

    public static GuestPreference Create(Guid guestId, PreferenceCategory category, string key, string value) =>
        new() { GuestId = guestId, Category = category, Key = key, Value = value };

    public void Update(string newValue) { Value = newValue; UpdatedAt = DateTime.UtcNow; }
}

public enum PreferenceCategory
{
    Room,           // Floor, view, bed type
    Dietary,        // Allergies, vegetarian, halal, kosher
    Pillow,         // Firm, soft, memory foam
    Temperature,    // AC setting
    Minibar,        // Stock preferences
    Activity,       // Safari type, spa preferences
    Communication,  // Email/SMS/WhatsApp preference
    Other
}

// ═══════════════════════════════════════════════════════════════════════
//  GUEST LOYALTY — Loyalty tier and point tracking
// ═══════════════════════════════════════════════════════════════════════
public class GuestLoyalty : Entity
{
    public Guid GuestId { get; private set; }
    public LoyaltyTier Tier { get; private set; } = LoyaltyTier.None;
    public int TotalPoints { get; private set; }
    public int AvailablePoints { get; private set; }
    public int TotalStays { get; private set; }
    public int TotalNights { get; private set; }
    public decimal TotalSpend { get; private set; }
    public DateTime? LastStayDate { get; private set; }
    public DateTime? TierExpiryDate { get; private set; }

    public Guest Guest { get; private set; } = null!;

    private GuestLoyalty() { }

    public static GuestLoyalty Create(Guid guestId) => new() { GuestId = guestId };

    public void RecordStay(int nights, decimal amount)
    {
        TotalStays++;
        TotalNights += nights;
        TotalSpend += amount;
        LastStayDate = DateTime.UtcNow;

        // Points: R1 = 1 point
        var pointsEarned = (int)Math.Floor(amount);
        TotalPoints += pointsEarned;
        AvailablePoints += pointsEarned;

        RecalculateTier();
        UpdatedAt = DateTime.UtcNow;
    }

    public bool RedeemPoints(int points)
    {
        if (points > AvailablePoints) return false;
        AvailablePoints -= points;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private void RecalculateTier()
    {
        Tier = TotalNights switch
        {
            >= 100 => LoyaltyTier.Platinum,
            >= 50  => LoyaltyTier.Gold,
            >= 20  => LoyaltyTier.Silver,
            >= 5   => LoyaltyTier.Bronze,
            _      => LoyaltyTier.None
        };
        TierExpiryDate = DateTime.UtcNow.AddYears(1);
    }
}

public enum LoyaltyTier { None, Bronze, Silver, Gold, Platinum }

// ═══════════════════════════════════════════════════════════════════════
//  NOTIFICATION — Communication queue (email, SMS, WhatsApp, push)
// ═══════════════════════════════════════════════════════════════════════
public class Notification : Entity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? RecipientGuestId { get; private set; }
    public Guid? RecipientStaffId { get; private set; }
    public string RecipientAddress { get; private set; } = string.Empty;    // Email or phone
    public NotificationChannel Channel { get; private set; }
    public NotificationType Type { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Queued;
    public DateTime? SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ExternalReference { get; private set; }                  // SendGrid / Clickatell ref

    private Notification() { }

    public static Notification Create(
        Guid propertyId, string recipientAddress, NotificationChannel channel,
        NotificationType type, string subject, string body,
        Guid? guestId = null, Guid? staffId = null)
    {
        return new Notification
        {
            PropertyId = propertyId,
            RecipientAddress = recipientAddress,
            Channel = channel,
            Type = type,
            Subject = subject,
            Body = body,
            RecipientGuestId = guestId,
            RecipientStaffId = staffId
        };
    }

    public void MarkSent(string? externalReference = null)
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalReference = externalReference;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        ErrorMessage = error;
        Status = RetryCount >= 3 ? NotificationStatus.Failed : NotificationStatus.Queued;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRead()
    {
        ReadAt = DateTime.UtcNow;
        Status = NotificationStatus.Read;
    }
}

public enum NotificationChannel { Email, SMS, WhatsApp, Push, InApp }
public enum NotificationType
{
    BookingConfirmation,
    BookingCancellation,
    CheckInReminder,
    CheckOutReminder,
    PaymentReceipt,
    InvoiceSent,
    ReviewRequest,
    SpecialOffer,
    StaffScheduleChange,
    MaintenanceAlert,
    SystemAlert
}
public enum NotificationStatus { Queued, Sending, Sent, Delivered, Read, Failed }

// ═══════════════════════════════════════════════════════════════════════
//  AUDIT LOG — System-wide change tracking
// ═══════════════════════════════════════════════════════════════════════
public class AuditLog : Entity
{
    public Guid? PropertyId { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;         // "Create", "Update", "Delete"
    public string EntityType { get; private set; } = string.Empty;     // "Booking", "Payment"
    public Guid EntityId { get; private set; }
    public string? OldValues { get; private set; }                     // JSON snapshot before
    public string? NewValues { get; private set; }                     // JSON snapshot after
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action, string entityType, Guid entityId,
        Guid? userId, string userName,
        string? oldValues = null, string? newValues = null,
        Guid? propertyId = null, string? ipAddress = null)
    {
        return new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserName = userName,
            OldValues = oldValues,
            NewValues = newValues,
            PropertyId = propertyId,
            IpAddress = ipAddress
        };
    }
}
