namespace SAFARIstack.API.Contracts.OTA;

/// <summary>OTA availability sync request</summary>
public record SyncAvailabilityRequest(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    List<string>? ChannelsToSync = null);

/// <summary>OTA availability sync response</summary>
public record SyncAvailabilityResponse(
    Guid PropertyId,
    int ChannelsChecked,
    int RoomsUpdated,
    decimal SyncPercentage,
    List<SyncErrorDto>? Errors,
    DateTime SyncedAt);

/// <summary>Publish availability request</summary>
public record PublishAvailabilityRequest(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    List<AvailabilityUpdateDto> Updates,
    bool PublishToAllChannels = true);

/// <summary>Publish availability response</summary>
public record PublishAvailabilityResponse(
    Guid PropertyId,
    int ChannelsUpdated,
    int RoomsPublished,
    List<ChannelStatusDto> ChannelStatuses,
    DateTime PublishedAt);

/// <summary>Pull bookings from OTA request</summary>
public record PullBookingsRequest(
    Guid PropertyId,
    DateTime? FromDate = null,
    List<string>? ChannelsToSync = null);

/// <summary>Pull bookings from OTA response</summary>
public record PullBookingsResponse(
    Guid PropertyId,
    int NewBookingsCreated,
    int BookingsUpdated,
    int ConflictsDetected,
    List<PulledBookingDto> Bookings,
    DateTime PulledAt);

/// <summary>Push booking status to OTA request</summary>
public record PushBookingStatusRequest(
    Guid BookingId,
    string BookingStatus,
    bool PushToAllChannels = true);

/// <summary>Push booking status to OTA response</summary>
public record PushBookingStatusResponse(
    Guid BookingId,
    int ChannelsPushed,
    List<ChannelStatusDto> ChannelStatuses,
    DateTime PushedAt);

/// <summary>Resolve conflicts request</summary>
public record ResolveConflictsRequest(
    Guid PropertyId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    string? ResolutionStrategy = null); // "keep_earliest", "keep_highest_rate", "manual_review"

/// <summary>Resolve conflicts response</summary>
public record ResolveConflictsResponse(
    Guid PropertyId,
    int ConflictsFound,
    int AutoResolvedCount,
    int ManualReviewRequired,
    List<ConflictDto> Conflicts,
    DateTime ResolvedAt);

/// <summary>Get availability snapshot response</summary>
public record AvailabilitySnapshotResponse(
    Guid PropertyId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    List<ChannelAvailabilityDto> ByChannel,
    RoomSummaryDto Summary,
    DateTime SnapshotAt);

/// <summary>Get connected channels response</summary>
public record ConnectedChannelsResponse(
    Guid PropertyId,
    List<ChannelInfoDto> Channels,
    int TotalConnected,
    DateTime CheckedAt);

/// <summary>Connect channel request</summary>
public record ConnectChannelRequest(
    int ChannelId,
    string ChannelName,
    string ApiKey,
    string? ApiSecret = null,
    Dictionary<string, string>? AdditionalConfig = null);

/// <summary>Connect channel response</summary>
public record ConnectChannelResponse(
    int ChannelId,
    string ChannelName,
    bool IsConnected,
    string Message,
    DateTime ConnectedAt);

/// <summary>Disconnect channel request</summary>
public record DisconnectChannelRequest(
    int ChannelId,
    bool ArchiveBookings = true);

/// <summary>Disconnect channel response</summary>
public record DisconnectChannelResponse(
    int ChannelId,
    string ChannelName,
    bool IsDisconnected,
    string Message,
    DateTime DisconnectedAt);

// ─── Supporting DTOs ─────────────────────────────────────────────────

/// <summary>Sync error detail</summary>
public record SyncErrorDto(
    string ChannelName,
    string ErrorCode,
    string ErrorMessage,
    DateTime OccurredAt);

/// <summary>Channel status for publish/push operations</summary>
public record ChannelStatusDto(
    string ChannelName,
    bool IsSuccessful,
    string? ErrorMessage = null,
    int ItemsProcessed = 0);

/// <summary>Pulled booking from OTA</summary>
public record PulledBookingDto(
    string ExternalReference,
    string GuestName,
    string GuestEmail,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Adults,
    int Children,
    decimal TotalPrice,
    string ChannelName,
    string SpecialRequests = "");

/// <summary>Conflict detail for resolution</summary>
public record ConflictDto(
    Guid? ExistingBookingId,
    string ExternalReference,
    string RoomType,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Guests,
    decimal RatePerNight,
    string Channel,
    string? ResolutionAction = null);

/// <summary>Availability update for publish</summary>
public record AvailabilityUpdateDto(
    string RoomType,
    DateTime Date,
    int AvailableRooms,
    decimal RatePerNight,
    int MinStayDays = 1,
    bool IsBlocked = false,
    string? BlockReasonReason = null);

/// <summary>Channel availability status</summary>
public record ChannelAvailabilityDto(
    string ChannelName,
    int AvailableRooms,
    int BookedRooms,
    int BlockedRooms,
    decimal LowestRate,
    decimal HighestRate,
    string SyncStatus); // "synced", "pending", "failed"

/// <summary>Room availability summary across channels</summary>
public record RoomSummaryDto(
    int TotalRooms,
    int FullyAvailable,
    int PartiallyAvailable,
    int FullyBooked,
    decimal AverageRate,
    string OverallStatusString); // "available", "limited", "fully_booked"

/// <summary>Connected channel information</summary>
public record ChannelInfoDto(
    int ChannelId,
    string ChannelName,
    bool IsConnected,
    DateTime? LastSyncAt,
    string? LastSyncStatus,
    DateTime? LastError = null,
    int HealthPercentage = 100);

/// <summary>OTA error response</summary>
public record OTAErrorResponse(
    string ErrorCode,
    string Message,
    string? Details = null,
    DateTime Timestamp = default);
