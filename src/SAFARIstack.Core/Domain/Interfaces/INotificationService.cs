using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Core.Domain.Interfaces;

// ═══════════════════════════════════════════════════════════════════════
//  NOTIFICATION SERVICE — Creates and queues notifications triggered
//  by guest and staff activities (bookings, payments, check-in/out, etc.)
// ═══════════════════════════════════════════════════════════════════════
public interface INotificationService
{
    /// <summary>
    /// Creates and queues a notification using the property's email template.
    /// Template placeholders are resolved from the provided values dictionary.
    /// Respects the property's notification preferences (e.g., SendBookingConfirmation).
    /// </summary>
    Task<Notification?> QueueNotificationAsync(
        Guid propertyId,
        NotificationType type,
        NotificationChannel channel,
        string recipientAddress,
        Dictionary<string, string> templateValues,
        Guid? guestId = null,
        Guid? staffId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a given notification type is enabled for the property.
    /// </summary>
    Task<bool> IsNotificationEnabledAsync(Guid propertyId, NotificationType type, CancellationToken ct = default);

    /// <summary>
    /// Gets queued notifications ready to send (batch).
    /// </summary>
    Task<IReadOnlyList<Notification>> GetQueuedNotificationsAsync(int batchSize = 50, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════
//  PROPERTY SETTINGS REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public interface IPropertySettingsRepository : IRepository<PropertySettings>
{
    Task<PropertySettings?> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════
//  EMAIL TEMPLATE REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate?> GetActiveTemplateAsync(Guid propertyId, NotificationType type, CancellationToken ct = default);
    Task<IReadOnlyList<EmailTemplate>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════
//  MERCHANT CONFIGURATION REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public interface IMerchantConfigurationRepository : IRepository<MerchantConfiguration>
{
    Task<IReadOnlyList<MerchantConfiguration>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task<MerchantConfiguration?> GetActiveByProviderAsync(Guid propertyId, MerchantProviderType providerType, CancellationToken ct = default);
}
