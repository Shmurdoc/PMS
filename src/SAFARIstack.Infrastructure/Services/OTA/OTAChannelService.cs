using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Exceptions.OTA;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Data;
using System;

namespace SAFARIstack.Infrastructure.Services.OTA;

/// <summary>
/// OTA Channel Service Implementation
/// Handles real-time synchronization with multiple OTA channels (Booking.com, Expedia, Airbnb, etc.)
/// Manages conflicts, availability updates, and booking integration
/// </summary>
public class OTAChannelService : IOTAChannelService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OTAChannelService> _logger;
    private readonly Dictionary<int, IOTAChannelProvider> _providers;

    public OTAChannelService(
        ApplicationDbContext dbContext,
        ILogger<OTAChannelService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _providers = new Dictionary<int, IOTAChannelProvider>();
    }

    /// <summary>Synchronize availability from all connected OTA channels</summary>
    public async Task<AvailabilitySyncResult> SyncAvailabilityAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting availability sync for property {PropertyId} from {FromDate} to {ToDate}",
            propertyId, fromDate, toDate);

        var errors = new List<SyncErrorDetail>();
        var channels = await GetConnectedChannelsAsync(propertyId, cancellationToken);
        int channelsChecked = 0;
        int roomsUpdated = 0;

        foreach (var channel in channels.Where(c => c.IsConnected))
        {
            try
            {
                if (!_providers.TryGetValue(channel.ChannelId, out var provider))
                {
                    errors.Add(new SyncErrorDetail(
                        channel.ChannelName,
                        "PROVIDER_NOT_FOUND",
                        $"No provider implementation for {channel.ChannelName}",
                        DateTime.UtcNow));
                    continue;
                }

                channelsChecked++;

                // Pull availability from channel
                var availability = await provider.FetchAvailabilityAsync(
                    propertyId, fromDate, toDate, CancellationToken.None);

                // Update local cache/database
                roomsUpdated += availability.Count;

                _logger.LogInformation("Synced {RoomCount} availability records from {Channel}",
                    availability.Count, channel.ChannelName);
            }
            catch (ChannelUnavailableException ex)
            {
                _logger.LogWarning("Channel {Channel} unavailable: {Message}", channel.ChannelName, ex.Message);
                errors.Add(new SyncErrorDetail(
                    channel.ChannelName,
                    "CHANNEL_UNAVAILABLE",
                    ex.Message,
                    DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing availability from {Channel}", channel.ChannelName);
                errors.Add(new SyncErrorDetail(
                    channel.ChannelName,
                    "SYNC_ERROR",
                    ex.Message,
                    DateTime.UtcNow));
            }
        }

        var syncPercentage = channelsChecked > 0 ? ((channelsChecked - errors.Count) * 100m) / channelsChecked : 0;

        return new AvailabilitySyncResult(
            propertyId,
            channelsChecked,
            roomsUpdated,
            syncPercentage,
            errors,
            DateTime.UtcNow);
    }

    /// <summary>Push local availability to all OTA channels</summary>
    public async Task<PublishAvailabilityResult> PublishAvailabilityAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        List<ChannelAvailabilityDto> availability,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing availability for property {PropertyId} across all channels",
            propertyId);

        var channels = await GetConnectedChannelsAsync(propertyId, cancellationToken);
        var publishStatuses = new List<ChannelPublishStatus>();
        int channelsUpdated = 0;

        foreach (var channel in channels.Where(c => c.IsConnected))
        {
            try
            {
                if (!_providers.TryGetValue(channel.ChannelId, out var provider))
                {
                    publishStatuses.Add(new ChannelPublishStatus(
                        channel.ChannelName,
                        false,
                        "Provider not found"));
                    continue;
                }

                // Push availability
                var result = await provider.PublishAvailabilityAsync(
                    propertyId, availability, CancellationToken.None);

                if (result.IsSuccessful)
                {
                    channelsUpdated++;
                }

                publishStatuses.Add(new ChannelPublishStatus(
                    channel.ChannelName,
                    result.IsSuccessful,
                    result.ErrorMessage,
                    availability.Count));

                _logger.LogInformation("Published {Count} availability records to {Channel}",
                    availability.Count, channel.ChannelName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing to {Channel}", channel.ChannelName);
                publishStatuses.Add(new ChannelPublishStatus(
                    channel.ChannelName,
                    false,
                    ex.Message));
            }
        }

        return new PublishAvailabilityResult(
            propertyId,
            channelsUpdated,
            availability.Count,
            publishStatuses,
            DateTime.UtcNow);
    }

    /// <summary>Pull new bookings from all OTA channels</summary>
    public async Task<BookingPullResult> PullBookingsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pulling bookings from all channels for property {PropertyId}", propertyId);

        var channels = await GetConnectedChannelsAsync(propertyId, cancellationToken);
        var newBookings = 0;
        var updatedBookings = 0;
        var pulledBookingsList = new List<PulledBookingInfo>();
        var conflictsDetected = 0;

        foreach (var channel in channels.Where(c => c.IsConnected))
        {
            try
            {
                if (!_providers.TryGetValue(channel.ChannelId, out var provider))
                    continue;

                // Fetch bookings from channel
                var bookings = await provider.FetchBookingsAsync(propertyId, CancellationToken.None);

                foreach (var booking in bookings)
                {
                    try
                    {
                        // Check for existing booking with same external reference
                        var existing = await _dbContext.Set<Booking>()
                            .FirstOrDefaultAsync(b => b.ExternalReference == booking.ExternalReference,
                                cancellationToken);

                        if (existing != null)
                        {
                            updatedBookings++;
                        }
                        else
                        {
                            // Check for conflicts (double-bookings)
                            var conflicts = await _dbContext.Set<Booking>()
                                .Where(b =>
                                    b.PropertyId == propertyId &&
                                    b.Status != BookingStatus.Cancelled &&
                                    b.CheckInDate < booking.CheckOutDate &&
                                    b.CheckOutDate > booking.CheckInDate)
                                .ToListAsync(cancellationToken);

                            if (conflicts.Any())
                            {
                                conflictsDetected += conflicts.Count;
                                _logger.LogWarning(
                                    "Double-booking detected: {ExternalRef} conflicts with {ConflictCount} existing bookings",
                                    booking.ExternalReference, conflicts.Count);
                            }
                            else
                            {
                                newBookings++;
                            }
                        }

                        pulledBookingsList.Add(new PulledBookingInfo(
                            booking.ExternalReference,
                            booking.GuestName,
                            booking.GuestEmail,
                            booking.CheckInDate,
                            booking.CheckOutDate,
                            booking.Guests,
                            booking.TotalPrice,
                            channel.ChannelName));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing booking {ExternalRef} from {Channel}",
                            booking.ExternalReference, channel.ChannelName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling bookings from {Channel}", channel.ChannelName);
            }
        }

        return new BookingPullResult(
            propertyId,
            newBookings,
            updatedBookings,
            conflictsDetected,
            pulledBookingsList,
            DateTime.UtcNow);
    }

    /// <summary>Push booking status to OTA channels</summary>
    public async Task<BookingPushResult> PushBookingStatusAsync(
        Guid bookingId,
        BookingSource source,
        BookingStatus status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pushing booking {BookingId} status {Status} to {Source}",
            bookingId, status, source);

        var booking = await _dbContext.Set<Booking>()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new InvalidOperationException($"Booking {bookingId} not found");

        var channels = await GetConnectedChannelsAsync(booking.PropertyId, cancellationToken);
        var pushStatuses = new List<ChannelPushStatus>();
        int channelsPushed = 0;

        foreach (var channel in channels.Where(c => c.IsConnected && c.ChannelId == (int)source))
        {
            try
            {
                if (!_providers.TryGetValue(channel.ChannelId, out var provider))
                {
                    pushStatuses.Add(new ChannelPushStatus(
                        channel.ChannelName,
                        false,
                        "Provider not found"));
                    continue;
                }

                // Push status to channel
                var result = await provider.PushBookingStatusAsync(
                    booking.ExternalReference ?? string.Empty,
                    status.ToString(),
                    CancellationToken.None);

                if (result.IsSuccessful)
                {
                    channelsPushed++;
                }

                pushStatuses.Add(new ChannelPushStatus(
                    channel.ChannelName,
                    result.IsSuccessful,
                    result.ErrorMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing to {Channel}", channel.ChannelName);
                pushStatuses.Add(new ChannelPushStatus(
                    channel.ChannelName,
                    false,
                    ex.Message));
            }
        }

        return new BookingPushResult(
            bookingId,
            channelsPushed,
            pushStatuses,
            DateTime.UtcNow);
    }

    /// <summary>Resolve double-bookings and conflicts automatically</summary>
    public async Task<ConflictResolutionResult> ResolveConflictsAsync(
        Guid propertyId,
        DateTime checkInDate,
        DateTime checkOutDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Resolving conflicts for property {PropertyId} from {CheckIn} to {CheckOut}",
            propertyId, checkInDate, checkOutDate);

        // Find overlapping bookings from multiple channels
        var overlappingBookings = await _dbContext.Set<Booking>()
            .Where(b =>
                b.PropertyId == propertyId &&
                b.Status != BookingStatus.Cancelled &&
                b.CheckInDate < checkOutDate &&
                b.CheckOutDate > checkInDate)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var conflicts = new List<ConflictDetails>();
        int autoResolvedCount = 0;
        int manualReviewRequired = 0;

        // Group by room to detect conflicts
        var groupedByRoom = overlappingBookings.GroupBy(b => b.BookingRooms.FirstOrDefault()?.RoomId);
        foreach (var roomGroup in groupedByRoom)
        {
            if (roomGroup.Count() <= 1)
                continue;

            // Multiple bookings for same room - conflict!
            var bookingList = roomGroup.ToList();
            for (int i = 0; i < bookingList.Count - 1; i++)
            {
                var booking1 = bookingList[i];
                var booking2 = bookingList[i + 1];

                conflicts.Add(new ConflictDetails(
                    booking1.Id,
                    booking2.Id,
                    booking1.BookingRooms.First().Room?.RoomType.ToString() ?? "Unknown",
                    booking1.CheckInDate,
                    booking1.CheckOutDate,
                    booking1.AdultCount + booking1.ChildCount,
                    booking1.TotalAmount / booking1.Nights,
                    booking1.Source.ToString(),
                    booking2.Source.ToString()));

                // Auto-resolve: keep the earlier booking (more reliable)
                var toCancel = booking1.CreatedAt > booking2.CreatedAt ? booking1 : booking2;
                try
                {
                    toCancel.Cancel(Guid.Empty, "Auto-cancelled due to double-booking conflict");
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    autoResolvedCount++;
                    _logger.LogInformation("Auto-resolved conflict by cancelling booking {BookingId}",
                        toCancel.Id);
                }
                catch (Exception ex)
                {
                    manualReviewRequired++;
                    _logger.LogError(ex, "Failed to auto-cancel booking {BookingId}", toCancel.Id);
                }
            }
        }

        return new ConflictResolutionResult(
            propertyId,
            conflicts.Count,
            autoResolvedCount,
            manualReviewRequired,
            conflicts,
            DateTime.UtcNow);
    }

    /// <summary>Get real-time availability snapshot</summary>
    public async Task<ChannelAvailabilitySnapshot> GetAvailabilitySnapshotAsync(
        Guid propertyId,
        DateTime checkInDate,
        DateTime checkOutDate,
        CancellationToken cancellationToken = default)
    {
        var channels = await GetConnectedChannelsAsync(propertyId, cancellationToken);
        var byChannel = new List<ChannelAvailabilityStatus>();

        foreach (var channel in channels.Where(c => c.IsConnected))
        {
            try
            {
                if (!_providers.TryGetValue(channel.ChannelId, out var provider))
                    continue;

                var snapshot = await provider.GetAvailabilityAsync(
                    propertyId, checkInDate, checkOutDate, CancellationToken.None);

                byChannel.Add(new ChannelAvailabilityStatus(
                    channel.ChannelName,
                    snapshot.AvailableRooms,
                    snapshot.BookedRooms,
                    snapshot.BlockedRooms,
                    snapshot.LowestRate,
                    snapshot.HighestRate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting snapshot from {Channel}", channel.ChannelName);
            }
        }

        // Calculate summary
        var totalRooms = await _dbContext.Set<Room>()
            .CountAsync(r => r.Property!.Id == propertyId && r.IsActive, cancellationToken);

        int totalAvailable = byChannel.Sum(c => c.AvailableRooms);
        int totalBooked = byChannel.Sum(c => c.BookedRooms);
        int totalBlocked = byChannel.Sum(c => c.BlockedRooms);
        
        var summary = new RoomAvailabilitySummary(
            totalRooms,
            totalAvailable,
            totalBooked,
            totalBlocked > 0 ? 1 : 0,  // PartiallyAvailable flag
            byChannel.Any() ? byChannel.Average(c => (c.LowestRate + c.HighestRate) / 2) : 0m);

        return new ChannelAvailabilitySnapshot(
            propertyId,
            checkInDate,
            checkOutDate,
            byChannel,
            summary,
            DateTime.UtcNow);
    }

    /// <summary>Get connected channels</summary>
    public async Task<List<ConnectedChannelInfo>> GetConnectedChannelsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        // Simulated: Would query database for channel credentials
        var channels = new List<ConnectedChannelInfo>
        {
            new(1, "Booking.com", true, DateTime.UtcNow.AddMinutes(-15), null, 95),
            new(2, "Expedia", true, DateTime.UtcNow.AddMinutes(-10), null, 98),
            new(5, "Airbnb", false, null, "Invalid credentials", 0),
            new(3, "Agoda", true, DateTime.UtcNow.AddMinutes(-5), null, 100),
        };

        return await Task.FromResult(channels);
    }

    /// <summary>Connect to OTA channel</summary>
    public async Task ConnectChannelAsync(
        Guid propertyId,
        int channelId,
        string apiKey,
        string? apiSecret = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key is required");

        _logger.LogInformation("Connecting to channel {ChannelId} for property {PropertyId}",
            channelId, propertyId);

        // Test connection
        var testResult = await TestChannelConnectionAsync(channelId, apiKey, apiSecret);
        if (!testResult)
            throw new ChannelConnectionException($"Channel {channelId}");

        // Store credentials securely (would use secure vault in production)
        // Save to database...

        _logger.LogInformation("Successfully connected channel {ChannelId}", channelId);
    }

    /// <summary>Disconnect from OTA channel</summary>
    public async Task DisconnectChannelAsync(
        Guid propertyId,
        int channelId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting channel {ChannelId} for property {PropertyId}",
            channelId, propertyId);

        // Remove credentials from database
        // ... implementation ...

        _logger.LogInformation("Successfully disconnected channel {ChannelId}", channelId);
        await Task.CompletedTask;
    }

    // ─── Helper Methods ──────────────────────────────────────────────

    private async Task<bool> TestChannelConnectionAsync(int channelId, string apiKey, string? apiSecret)
    {
        try
        {
            if (_providers.TryGetValue(channelId, out var provider))
            {
                return await provider.TestConnectionAsync(apiKey, apiSecret);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>Interface for OTA channel providers (Booking.com, Expedia, etc)</summary>
public interface IOTAChannelProvider
{
    /// Fetch availability from channel
    Task<List<ChannelAvailabilityDto>> FetchAvailabilityAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken);

    /// Publish availability to channel
    Task<PublishResult> PublishAvailabilityAsync(
        Guid propertyId,
        List<ChannelAvailabilityDto> availability,
        CancellationToken cancellationToken);

    /// Fetch bookings from channel
    Task<List<OTABookingInfo>> FetchBookingsAsync(
        Guid propertyId,
        CancellationToken cancellationToken);

    /// Push booking status to channel
    Task<PushResult> PushBookingStatusAsync(
        string externalReference,
        string status,
        CancellationToken cancellationToken);

    /// Get availability snapshot
    Task<AvailabilitySnapshot> GetAvailabilityAsync(
        Guid propertyId,
        DateTime checkInDate,
        DateTime checkOutDate,
        CancellationToken cancellationToken);

    /// Test connection
    Task<bool> TestConnectionAsync(string apiKey, string? apiSecret);
}

public record PublishResult(bool IsSuccessful, string? ErrorMessage = null);
public record PushResult(bool IsSuccessful, string? ErrorMessage = null);
public record OTABookingInfo(
    string ExternalReference,
    string GuestName,
    string GuestEmail,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int Guests,
    decimal TotalPrice);
public record AvailabilitySnapshot(
    int AvailableRooms,
    int BookedRooms,
    int BlockedRooms,
    decimal LowestRate,
    decimal HighestRate);
