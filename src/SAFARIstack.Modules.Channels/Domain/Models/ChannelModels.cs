namespace SAFARIstack.Modules.Channels.Domain.Models;

/// <summary>
/// OTA channel configuration
/// </summary>
public class OTAChannelConfig
{
    public Guid ChannelId { get; init; } = Guid.NewGuid();
    public Guid PropertyId { get; init; }
    public string ChannelName { get; init; } = string.Empty; // "booking.com", "expedia", "airbnb"
    public string ApiKey { get; init; } = string.Empty;
    public string ApiEndpoint { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public DateTime LastSyncTime { get; init; }
    public string SyncStrategy { get; init; } = "delta"; // "full" or "delta"
}

/// <summary>
/// Channel sync status and metrics
/// </summary>
public class ChannelSyncStatus
{
    public Guid ChannelId { get; init; }
    public DateTime LastSuccessfulSync { get; init; }
    public int TotalSyncAttempts { get; init; }
    public int SuccessfulSyncs { get; init; }
    public int FailedSyncs { get; init; }
    public string? LastErrorMessage { get; init; }
    public int AverageResyncTimeMs { get; init; }
}

/// <summary>
/// Delta update for efficient OTA sync
/// Only sends changed data to OTA (availability, rate, restrictions)
/// </summary>
public class DeltaUpdate
{
    public Guid PropertyId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public DateRange AffectedPeriod { get; init; }
    public Dictionary<Guid, int> AvailabilityChanges { get; init; } = new(); // RoomId -> NewAvailable
    public Dictionary<Guid, decimal> RateChanges { get; init; } = new(); // RoomTypeId -> NewRate
    public Dictionary<Guid, RestrictionUpdate[]> RestrictionChanges { get; init; } = new();
}

/// <summary>
/// Restriction update (minimum stay, closed-to-arrival, etc.)
/// </summary>
public class RestrictionUpdate
{
    public string RestrictionType { get; init; } = string.Empty; // "MinStay", "ClosedToArrival", "ClosedToCheckout"
    public DateTime EffectiveDate { get; init; }
    public int? Value { get; init; } // For MinStay: number of nights
    public bool IsActive { get; init; }
}

/// <summary>
/// Overbooking conflict detected by channel manager
/// </summary>
public class OverbookingConflict
{
    public Guid ConflictId { get; init; } = Guid.NewGuid();
    public Guid PropertyId { get; init; }
    public DateTime ConflictDate { get; init; }
    public Guid RoomTypeId { get; init; }
    public int BookedRooms { get; init; }
    public int AvailableRooms { get; init; }
    public string[] ChannelsInvolved { get; init; } = Array.Empty<string>();
    public string ResolutionStrategy { get; init; } = "automatic"; // "automatic" or "manual_review"
}

/// <summary>
/// Value object for date ranges
/// </summary>
public class DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");
        StartDate = startDate;
        EndDate = endDate;
    }
}
