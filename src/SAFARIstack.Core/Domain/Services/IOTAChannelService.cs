using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Core.Domain.Services;

/// <summary>
/// OTA (Online Travel Agency) multi-channel booking synchronization service
/// Handles real-time sync with Booking.com, Expedia, Airbnb, Agoda, etc.
/// </summary>
public interface IOTAChannelService
{
    /// <summary>
    /// Synchronize availability from all connected OTA channels
    /// </summary>
    /// <remarks>
    /// Pulls current availability for all properties across all connected channels.
    /// Updates local cache with rates, restrictions, and occupancy.
    /// Typically run every 15-30 minutes.
    /// </remarks>
    Task<AvailabilitySyncResult> SyncAvailabilityAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Push local availability to all OTA channels
    /// </summary>
    /// <remarks>
    /// Publishes property availability, rates, and restrictions to connected OTAs.
    /// Triggered when: inventory updated, rates changed, availability modified.
    /// Updates all channel calendars with 5-minute syncing window.
    /// </remarks>
    Task<PublishAvailabilityResult> PublishAvailabilityAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        List<ChannelAvailabilityDto> availability,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pull new bookings from all OTA channels into system
    /// </summary>
    /// <remarks>
    /// Fetches all new/modified bookings from configured OTA channels.
    /// Creates or updates local Booking records with external references.
    /// Generates BookingReceivedFromOTA events for webhook integration.
    /// Run frequency: Every 5-10 minutes for real-time sync.
    /// </remarks>
    Task<BookingPullResult> PullBookingsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Push booking status changes to OTA channels
    /// </summary>
    /// <remarks>
    /// Synchronizes local booking status (confirmed, cancelled, checked-in) to OTAs.
    /// Called when: booking confirmed, cancelled, updated, checked-in/out.
    /// Ensures consistent guest communication across all channels.
    /// </remarks>
    Task<BookingPushResult> PushBookingStatusAsync(
        Guid bookingId,
        BookingSource source,
        BookingStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve double-bookings and conflicts automatically
    /// </summary>
    /// <remarks>
    /// Detects overlapping bookings from multiple channels on same room.
    /// Applies conflict resolution strategy: Keep earliest, higher rate, or manual review.
    /// Automatically cancels/rejects conflicting bookings with guest notification.
    /// Returns detailed conflict report with resolution actions taken.
    /// </remarks>
    Task<ConflictResolutionResult> ResolveConflictsAsync(
        Guid propertyId,
        DateTime checkInDate,
        DateTime checkOutDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time availability snapshot across channels
    /// </summary>
    /// <remarks>
    /// Returns current availability status for property across all channels.
    /// Shows: available rooms, booked rooms, blocked dates, rate variations.
    /// Cached for 5 minutes, real-time update available on demand.
    /// </remarks>
    Task<ChannelAvailabilitySnapshot> GetAvailabilitySnapshotAsync(
        Guid propertyId,
        DateTime checkInDate,
        DateTime checkOutDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configured OTA channels and their status
    /// </summary>
    /// <remarks>
    /// Returns list of connected OTA channels with connection status, last sync, error info.
    /// Used for dashboard health monitoring and channel management.
    /// </remarks>
    Task<List<ConnectedChannelInfo>> GetConnectedChannelsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Connect/authenticate to new OTA channel
    /// </summary>
    /// <remarks>
    /// Stores API credentials for new OTA channel (Booking.com, Expedia, etc).
    /// Tests connection and validates credentials before saving.
    /// Triggers initial availability sync once connection verified.
    /// </remarks>
    Task ConnectChannelAsync(
        Guid propertyId,
        int channelId,
        string apiKey,
        string? apiSecret = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from OTA channel
    /// </summary>
    /// <remarks>
    /// Removes API credentials and stops syncing for specified channel.
    /// Existing bookings remain in system with historical reference.
    /// Can be reconnected later with new credentials.
    /// </remarks>
    Task DisconnectChannelAsync(
        Guid propertyId,
        int channelId,
        CancellationToken cancellationToken = default);
}

// ─── Supporting Records ───────────────────────────────────────────────

/// <summary>Availability sync result from OTA pull</summary>
public record AvailabilitySyncResult(
    Guid PropertyId,
    int ChannelsChecked,
    int RoomsUpdated,
    decimal SyncPercentage,
    List<SyncErrorDetail> Errors,
    DateTime SyncedAt);

/// <summary>Availability publish result to all OTAs</summary>
public record PublishAvailabilityResult(
    Guid PropertyId,
    int ChannelsUpdated,
    int RoomsPublished,
    List<ChannelPublishStatus> ChannelStatuses,
    DateTime PublishedAt);

/// <summary>Booking pull result from OTA channels</summary>
public record BookingPullResult(
    Guid PropertyId,
    int NewBookingsCreated,
    int BookingsUpdated,
    int ConflictsDetected,
    List<PulledBookingInfo> Bookings,
    DateTime PulledAt);

/// <summary>Booking push result to OTA channels</summary>
public record BookingPushResult(
    Guid BookingId,
    int ChannelsPushed,
    List<ChannelPushStatus> ChannelStatuses,
    DateTime PushedAt);

/// <summary>Conflict resolution result with actions taken</summary>
public record ConflictResolutionResult(
    Guid PropertyId,
    int ConflictsFound,
    int AutoResolvedCount,
    int ManualReviewRequired,
    List<ConflictDetails> Conflicts,
    DateTime ResolvedAt);

/// <summary>Snapshot of availability across all channels</summary>
public record ChannelAvailabilitySnapshot(
    Guid PropertyId,
    DateTime CheckinDate,
    DateTime CheckoutDate,
    List<ChannelAvailabilityStatus> ByChannel,
    RoomAvailabilitySummary Summary,
    DateTime SnapshotAt);

/// <summary>Information about connected OTA channel</summary>
public record ConnectedChannelInfo(
    int ChannelId,
    string ChannelName,
    bool IsConnected,
    DateTime? LastSyncAt,
    string? LastError,
    int SyncHealthPercentage);

// ─── Supporting DTOs ───────────────────────────────────────────────────

/// <summary>Availability data for a single date range on a channel</summary>
public record ChannelAvailabilityDto(
    string RoomType,
    DateTime Date,
    int AvailableRooms,
    decimal RatePerNight,
    decimal MinStay,
    bool IsBlocked,
    string? RestrictionReason = null);

/// <summary>Channel publish status for individual channel</summary>
public record ChannelPublishStatus(
    string ChannelName,
    bool IsSuccessful,
    string? ErrorMessage = null,
    int RoomsPublished = 0);

/// <summary>Details about individual pulled booking</summary>
public record PulledBookingInfo(
    string ExternalReference,
    string GuestName,
    string GuestEmail,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Guests,
    decimal TotalPrice,
    string ChannelName);

/// <summary>Status for pushing booking to channel</summary>
public record ChannelPushStatus(
    string ChannelName,
    bool IsSuccessful,
    string? ErrorMessage = null);

/// <summary>Conflicting booking details for resolution</summary>
public record ConflictDetails(
    Guid BookingId1,
    Guid? BookingId2,
    string RoomType,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int GuestsCount,
    decimal RatePerNight,
    string Channel1,
    string? Channel2 = null,
    string? ResolutionAction = null);

/// <summary>Availability status on single channel</summary>
public record ChannelAvailabilityStatus(
    string ChannelName,
    int AvailableRooms,
    int BookedRooms,
    int BlockedRooms,
    decimal LowestRate,
    decimal HighestRate);

/// <summary>Summary of room availability across all channels</summary>
public record RoomAvailabilitySummary(
    int TotalRooms,
    int AvailableAcrossChannels,
    int FullyBooked,
    int PartiallyAvailable,
    decimal AverageRate);

/// <summary>Sync error details for logging/monitoring</summary>
public record SyncErrorDetail(
    string ChannelName,
    string ErrorCode,
    string ErrorMessage,
    DateTime OccurredAt);
