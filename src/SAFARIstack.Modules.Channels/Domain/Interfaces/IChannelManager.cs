namespace SAFARIstack.Modules.Channels.Domain.Interfaces;

using SAFARIstack.Modules.Channels.Domain.Models;

/// <summary>
/// Channel Manager service contract
/// Synchronizes availability, rates, and restrictions with OTAs
/// 2-way, near-real-time updates with conflict resolution
/// Consumed by Revenue Management System and Property Management
/// </summary>
public interface IChannelManager
{
    /// <summary>
    /// Sync availability changes to OTA (delta-based)
    /// Only sends rooms that changed, not entire inventory
    /// </summary>
    Task<bool> SyncAvailabilityAsync(Guid propertyId, string channel, DeltaUpdate delta, CancellationToken ct = default);

    /// <summary>
    /// Sync rate changes to OTA (delta-based)
    /// </summary>
    Task<bool> SyncRatesAsync(Guid propertyId, string channel, DeltaUpdate delta, CancellationToken ct = default);

    /// <summary>
    /// Sync restrictions (MinStay, ClosedToArrival, etc.) to OTA
    /// </summary>
    Task<bool> SyncRestrictionsAsync(Guid propertyId, string channel, DeltaUpdate delta, CancellationToken ct = default);

    /// <summary>
    /// Detect and resolve overbooking conflicts
    /// Prevents double-booking across channels
    /// </summary>
    Task<OverbookingConflict?> DetectOverbookingAsync(Guid propertyId, DateTime date, Guid roomTypeId, CancellationToken ct = default);

    /// <summary>
    /// Get sync status for a channel
    /// </summary>
    Task<ChannelSyncStatus> GetSyncStatusAsync(Guid channelId, CancellationToken ct = default);

    /// <summary>
    /// Manually trigger full sync (used for recovery)
    /// </summary>
    Task<bool> FullSyncAsync(Guid propertyId, string channel, CancellationToken ct = default);
}

/// <summary>
/// OTA API client abstraction
/// Implemented per OTA (Booking.com, Expedia, Airbnb, etc.)
/// </summary>
public interface IOTAClient
{
    string ChannelName { get; }
    Task<bool> UpdateAvailabilityAsync(DeltaUpdate delta, CancellationToken ct = default);
    Task<bool> UpdateRatesAsync(DeltaUpdate delta, CancellationToken ct = default);
    Task<bool> UpdateRestrictionsAsync(DeltaUpdate delta, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> FetchAvailabilityAsync(Guid propertyId, DateRange period, CancellationToken ct = default);
    Task<Dictionary<Guid, decimal>> FetchRatesAsync(Guid propertyId, DateRange period, CancellationToken ct = default);
}

/// <summary>
/// Conflict resolution engine
/// Prevents overbooking using optimistic locking or consensus algorithms
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Resolve overbooking by applying conflict resolution rules
    /// </summary>
    Task<OverbookingConflict?> ResolveAsync(OverbookingConflict conflict, CancellationToken ct = default);

    /// <summary>
    /// Get conflict resolution rules for property
    /// </summary>
    Task<Dictionary<string, string>> GetRulesAsync(Guid propertyId, CancellationToken ct = default);
}
