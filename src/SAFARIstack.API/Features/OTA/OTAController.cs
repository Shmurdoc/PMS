using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAFARIstack.API.Contracts.OTA;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Exceptions.OTA;
using SAFARIstack.Core.Domain.Services;

namespace SAFARIstack.API.Features.OTA;

/// <summary>
/// OTA Channel Management & Synchronization REST API
/// Endpoints for managing multi-channel booking synchronization with Booking.com, Expedia, Airbnb, etc.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OTAController : ControllerBase
{
    private readonly IOTAChannelService _otaService;
    private readonly ILogger<OTAController> _logger;

    public OTAController(
        IOTAChannelService otaService,
        ILogger<OTAController> logger)
    {
        _otaService = otaService;
        _logger = logger;
    }

    /// <summary>
    /// Synchronize availability from all OTA channels
    /// </summary>
    /// <remarks>
    /// Pulls current availability/rates/restrictions from all connected OTA channels.
    /// Typically called every 15-30 minutes for real-time sync.
    /// </remarks>
    [HttpPost("sync-availability")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(SyncAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SyncAvailabilityResponse>> SyncAvailability(
        [FromBody] SyncAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Initiating availability sync for property {PropertyId}",
                request.PropertyId);

            var result = await _otaService.SyncAvailabilityAsync(
                request.PropertyId,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            var response = new SyncAvailabilityResponse(
                result.PropertyId,
                result.ChannelsChecked,
                result.RoomsUpdated,
                result.SyncPercentage,
                result.Errors.Select(e => new SyncErrorDto(
                    e.ChannelName,
                    e.ErrorCode,
                    e.ErrorMessage,
                    e.OccurredAt)).ToList(),
                result.SyncedAt);

            return Ok(response);
        }
        catch (ChannelUnavailableException ex)
        {
            _logger.LogWarning("Channel unavailable: {Message}", ex.Message);
            return StatusCode(503, new OTAErrorResponse(
                ex.ErrorCode,
                ex.Message,
                "One or more OTA channels are unavailable. Retry in 5 minutes."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing OTA availability");
            return StatusCode(500, new OTAErrorResponse(
                "SYNC_FAILED",
                "Failed to sync availability from OTA channels",
                ex.Message));
        }
    }

    /// <summary>
    /// Publish local availability to all OTA channels
    /// </summary>
    /// <remarks>
    /// Pushes property availability, rates, and restrictions to all connected OTA channels.
    /// Call after: inventory updated, rates changed, availability modified, or blocks added.
    /// </remarks>
    [HttpPost("publish-availability")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(PublishAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublishAvailabilityResponse>> PublishAvailability(
        [FromBody] PublishAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Publishing availability for property {PropertyId}",
                request.PropertyId);

            var result = await _otaService.PublishAvailabilityAsync(
                request.PropertyId,
                request.FromDate,
                request.ToDate,
                request.Updates.Select(u => new Core.Domain.Services.ChannelAvailabilityDto(
                    u.RoomType,
                    u.Date,
                    u.AvailableRooms,
                    u.RatePerNight,
                    u.MinStayDays,
                    u.IsBlocked,
                    u.BlockReasonReason)).ToList(),
                cancellationToken);

            var response = new PublishAvailabilityResponse(
                result.PropertyId,
                result.ChannelsUpdated,
                result.RoomsPublished,
                result.ChannelStatuses.Select(s => new ChannelStatusDto(
                    s.ChannelName,
                    s.IsSuccessful,
                    s.ErrorMessage,
                    s.RoomsPublished)).ToList(),
                result.PublishedAt);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OTA availability");
            return BadRequest(new OTAErrorResponse(
                "PUBLISH_FAILED",
                "Failed to publish availability to OTA channels",
                ex.Message));
        }
    }

    /// <summary>
    /// Pull new bookings from all OTA channels
    /// </summary>
    /// <remarks>
    /// Fetches all new/modified bookings from connected OTA channels.
    /// Creates or updates local Booking records with external references.
    /// Detects and reports double-bookings.
    /// Typical run: Every 5-10 minutes for real-time sync.
    /// </remarks>
    [HttpPost("pull-bookings")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(PullBookingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PullBookingsResponse>> PullBookings(
        [FromBody] PullBookingsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Pulling bookings for property {PropertyId}",
                request.PropertyId);

            var result = await _otaService.PullBookingsAsync(
                request.PropertyId,
                cancellationToken);

            var response = new PullBookingsResponse(
                result.PropertyId,
                result.NewBookingsCreated,
                result.BookingsUpdated,
                result.ConflictsDetected,
                result.Bookings.Select(b => new PulledBookingDto(
                    b.ExternalReference,
                    b.GuestName,
                    b.GuestEmail,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.Guests,  // Adults
                    0,  // Children (not specified in OTA pull)
                    b.TotalPrice,
                    b.ChannelName)).ToList(),
                result.PulledAt);

            return Ok(response);
        }
        catch (ChannelUnavailableException ex)
        {
            return StatusCode(503, new OTAErrorResponse(
                ex.ErrorCode,
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling OTA bookings");
            return StatusCode(500, new OTAErrorResponse(
                "PULL_FAILED",
                "Failed to pull bookings from OTA channels",
                ex.Message));
        }
    }

    /// <summary>
    /// Push booking status to OTA channels
    /// </summary>
    /// <remarks>
    /// Synchronizes local booking status changes (confirmed, cancelled, checked-in, etc.) to OTA channels.
    /// Ensures consistent guest communication across all channels.
    /// Called automatically on: confirmation, cancellation, check-in, check-out.
    /// </remarks>
    [HttpPost("push-booking-status")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(PushBookingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PushBookingStatusResponse>> PushBookingStatus(
        [FromBody] PushBookingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Pushing booking {BookingId} status {Status}",
                request.BookingId, request.BookingStatus);

            // Parse booking status
            if (!Enum.TryParse<BookingStatus>(request.BookingStatus, true, out var status))
            {
                return BadRequest(new OTAErrorResponse(
                    "INVALID_STATUS",
                    $"Invalid booking status: {request.BookingStatus}"));
            }

            var result = await _otaService.PushBookingStatusAsync(
                request.BookingId,
                BookingSource.Direct,
                status,
                cancellationToken);

            var response = new PushBookingStatusResponse(
                result.BookingId,
                result.ChannelsPushed,
                result.ChannelStatuses.Select(s => new ChannelStatusDto(
                    s.ChannelName,
                    s.IsSuccessful,
                    s.ErrorMessage)).ToList(),
                result.PushedAt);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing booking status");
            return BadRequest(new OTAErrorResponse(
                "PUSH_FAILED",
                "Failed to push booking status to OTA channels",
                ex.Message));
        }
    }

    /// <summary>
    /// Resolve double-bookings and booking conflicts
    /// </summary>
    /// <remarks>
    /// Detects overlapping bookings from multiple OTA channels on the same room.
    /// Automatically resolves conflicts based on strategy: keep_earliest (default), keep_highest_rate, manual_review.
    /// Cancels conflicting bookings with automatic guest notification.
    /// </remarks>
    [HttpPost("resolve-conflicts")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(ResolveConflictsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResolveConflictsResponse>> ResolveConflicts(
        [FromBody] ResolveConflictsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Resolving conflicts for property {PropertyId} from {CheckIn} to {CheckOut}",
                request.PropertyId, request.CheckInDate, request.CheckOutDate);

            var result = await _otaService.ResolveConflictsAsync(
                request.PropertyId,
                request.CheckInDate,
                request.CheckOutDate,
                cancellationToken);

            var response = new ResolveConflictsResponse(
                result.PropertyId,
                result.ConflictsFound,
                result.AutoResolvedCount,
                result.ManualReviewRequired,
                result.Conflicts.Select(c => new ConflictDto(
                    c.BookingId1,
                    c.BookingId2?.ToString() ?? "auto-detected",
                    c.RoomType,
                    c.CheckInDate,
                    c.CheckOutDate,
                    c.GuestsCount,
                    c.RatePerNight,
                    c.Channel1,
                    c.ResolutionAction)).ToList(),
                result.ResolvedAt);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving OTA conflicts");
            return BadRequest(new OTAErrorResponse(
                "CONFLICT_RESOLUTION_FAILED",
                "Failed to resolve booking conflicts",
                ex.Message));
        }
    }

    /// <summary>
    /// Get real-time availability snapshot across all channels
    /// </summary>
    /// <remarks>
    /// Returns current availability status across all connected OTA channels.
    /// Shows: available rooms, booked rooms, blocked dates, rate variations.
    /// Cached for 5 minutes, supports real-time refresh on demand.
    /// </remarks>
    [HttpGet("availability-snapshot")]
    [ProducesResponseType(typeof(AvailabilitySnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilitySnapshotResponse>> GetAvailabilitySnapshot(
        [FromQuery] Guid propertyId,
        [FromQuery] DateTime checkInDate,
        [FromQuery] DateTime checkOutDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Getting availability snapshot for property {PropertyId}",
                propertyId);

            var result = await _otaService.GetAvailabilitySnapshotAsync(
                propertyId,
                checkInDate,
                checkOutDate,
                cancellationToken);

            var response = new AvailabilitySnapshotResponse(
                result.PropertyId,
                result.CheckinDate,
                result.CheckoutDate,
                result.ByChannel.Select(c => new Contracts.OTA.ChannelAvailabilityDto(
                    c.ChannelName,
                    c.AvailableRooms,
                    c.BookedRooms,
                    c.BlockedRooms,
                    c.LowestRate,
                    c.HighestRate,
                    "synced")).ToList(),
                new RoomSummaryDto(
                    result.Summary.TotalRooms,
                    result.Summary.AvailableAcrossChannels,
                    result.Summary.PartiallyAvailable,
                    result.Summary.FullyBooked,
                    result.Summary.AverageRate,
                    result.Summary.TotalRooms == 0 ? "no_rooms" :
                    result.Summary.AvailableAcrossChannels == 0 ? "fully_booked" :
                    result.Summary.AvailableAcrossChannels < result.Summary.TotalRooms / 2 ? "limited" :
                    "available"),
                result.SnapshotAt);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting availability snapshot");
            return BadRequest(new OTAErrorResponse(
                "SNAPSHOT_FAILED",
                "Failed to get availability snapshot",
                ex.Message));
        }
    }

    /// <summary>
    /// Get list of connected OTA channels
    /// </summary>
    /// <remarks>
    /// Lists all configured OTA channel connections with status, last sync time, and health metrics.
    /// Used for dashboard monitoring and channel management.
    /// </remarks>
    [HttpGet("channels")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(ConnectedChannelsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectedChannelsResponse>> GetConnectedChannels(
        [FromQuery] Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var channels = await _otaService.GetConnectedChannelsAsync(propertyId, cancellationToken);

            var response = new ConnectedChannelsResponse(
                propertyId,
                channels.Select(c => new ChannelInfoDto(
                    c.ChannelId,
                    c.ChannelName,
                    c.IsConnected,
                    c.LastSyncAt,
                    c.IsConnected ? "synced" : "not_connected",
                    c.LastError != null ? DateTime.UtcNow : null,
                    c.SyncHealthPercentage)).ToList(),
                channels.Count(c => c.IsConnected),
                DateTime.UtcNow);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connected channels");
            return StatusCode(500, new OTAErrorResponse(
                "CHANNELS_FAILED",
                "Failed to retrieve connected channels",
                ex.Message));
        }
    }

    /// <summary>
    /// Connect to new OTA channel
    /// </summary>
    /// <remarks>
    /// Authenticates with OTA channel (Booking.com, Expedia, Airbnb, etc.)
    /// Stores API credentials securely and performs initial availability sync.
    /// Tests connection before saving credentials.
    /// </remarks>
    [HttpPost("channels/connect")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(ConnectChannelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ConnectChannelResponse>> ConnectChannel(
        [FromBody] ConnectChannelRequest request,
        [FromQuery] Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Connecting channel {ChannelName} for property {PropertyId}",
                request.ChannelName, propertyId);

            await _otaService.ConnectChannelAsync(
                propertyId,
                request.ChannelId,
                request.ApiKey,
                request.ApiSecret,
                cancellationToken);

            return Ok(new ConnectChannelResponse(
                request.ChannelId,
                request.ChannelName,
                true,
                $"Successfully connected to {request.ChannelName}",
                DateTime.UtcNow));
        }
        catch (ChannelConnectionException ex)
        {
            return StatusCode(503, new OTAErrorResponse(
                ex.ErrorCode,
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting channel");
            return BadRequest(new OTAErrorResponse(
                "CONNECT_FAILED",
                "Failed to connect to OTA channel",
                ex.Message));
        }
    }

    /// <summary>
    /// Disconnect from OTA channel
    /// </summary>
    /// <remarks>
    /// Removes API credentials and stops syncing for specified channel.
    /// Existing bookings remain in system with historical reference.
    /// Can be reconnected later with new credentials.
    /// </remarks>
    [HttpPost("channels/disconnect")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(DisconnectChannelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OTAErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DisconnectChannelResponse>> DisconnectChannel(
        [FromBody] DisconnectChannelRequest request,
        [FromQuery] Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("OTA: Disconnecting channel {ChannelId} for property {PropertyId}",
                request.ChannelId, propertyId);

            await _otaService.DisconnectChannelAsync(propertyId, request.ChannelId, cancellationToken);

            var channelName = request.ChannelId switch
            {
                1 => "Booking.com",
                2 => "Expedia",
                3 => "Agoda",
                5 => "Airbnb",
                _ => $"Channel {request.ChannelId}"
            };

            return Ok(new DisconnectChannelResponse(
                request.ChannelId,
                channelName,
                true,
                $"Successfully disconnected from {channelName}",
                DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting channel");
            return BadRequest(new OTAErrorResponse(
                "DISCONNECT_FAILED",
                "Failed to disconnect from OTA channel",
                ex.Message));
        }
    }
}
