using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SAFARIstack.Modules.Channels.Domain.Interfaces;
using SAFARIstack.Modules.Channels.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SAFARIstack.API.Endpoints.Channels;

/// <summary>
/// OTA Channel Manager endpoints for synchronizing with booking channels
/// (Booking.com, Expedia, Airbnb, Agoda, etc.)
/// </summary>
public static class ChannelEndpoints
{
    public static void MapChannelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/channels")
            .WithTags("OTA Channels")
            .RequireAuthorization("ManagerOrAbove");

        // ═══════════════════════════════════════════════════════════════
        //  SYNCHRONIZATION ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Trigger immediate delta-based sync with configured OTA channels
        /// </summary>
        group.MapPost("/sync", TriggerChannelSync)
            .WithName("TriggerChannelSync")
            .WithOpenApi()
            .WithSummary("Trigger OTA channel sync")
            .Produces(StatusCodes.Status202Accepted);

        /// <summary>
        /// Get status of last sync operation for a channel
        /// </summary>
        group.MapGet("/status/{channelId}", GetSyncStatus)
            .WithName("GetSyncStatus")
            .WithOpenApi()
            .WithSummary("Get OTA sync status for channel")
            .Produces<ChannelSyncStatusDto>(StatusCodes.Status200OK);

        // ═══════════════════════════════════════════════════════════════
        //  CONFLICT MANAGEMENT ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Check for overbooking conflicts for a specific date and room
        /// </summary>
        group.MapGet("/check-overbooking", CheckOverbooking)
            .WithName("CheckOverbooking")
            .WithOpenApi()
            .WithSummary("Detect overbooking conflicts")
            .Produces<OverbookingConflictDto?>(StatusCodes.Status200OK);

        // ═══════════════════════════════════════════════════════════════
        //  FULL SYNC ENDPOINTS (Recovery/Reconciliation)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Perform full inventory sync (not delta-based)
        /// Used for recovery when delta syncs get out of sync
        /// </summary>
        group.MapPost("/full-sync", PerformFullSync)
            .WithName("PerformFullSync")
            .WithOpenApi()
            .WithSummary("Perform full inventory reconciliation with OTA")
            .Produces(StatusCodes.Status202Accepted);
    }

    // ─── Handler Implementations ────────────────────────────────────

    private static async Task<IResult> TriggerChannelSync(
        [FromQuery] Guid propertyId,
        [FromQuery] string channel,
        IChannelManager channelManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a minimal delta update for sync trigger
            var delta = new DeltaUpdate
            {
                AffectedPeriod = new DateRange(DateTime.UtcNow, DateTime.UtcNow.AddDays(90)),
                AvailabilityChanges = new Dictionary<Guid, int>(),
                RateChanges = new Dictionary<Guid, decimal>()
            };

            var result = await channelManager.SyncAvailabilityAsync(propertyId, channel, delta, cancellationToken);
            return result 
                ? Results.Accepted($"/api/channels/status/{channel}", 
                    new { Message = "Sync initiated", Channel = channel, PropertyId = propertyId })
                : Results.BadRequest(new { Error = "Sync failed to start" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> GetSyncStatus(
        Guid channelId,
        IChannelManager channelManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await channelManager.GetSyncStatusAsync(channelId, cancellationToken);
            return Results.Ok(new
            {
                status.ChannelId,
                status.LastSuccessfulSync,
                status.TotalSyncAttempts,
                status.SuccessfulSyncs,
                status.FailedSyncs,
                SuccessRate = status.TotalSyncAttempts > 0 
                    ? (double)status.SuccessfulSyncs / status.TotalSyncAttempts 
                    : 0,
                status.AverageResyncTimeMs,
                status.LastErrorMessage
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> CheckOverbooking(
        [FromQuery] Guid propertyId,
        [FromQuery] DateTime date,
        [FromQuery] Guid roomTypeId,
        IChannelManager channelManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conflict = await channelManager.DetectOverbookingAsync(propertyId, date, roomTypeId, cancellationToken);
            if (conflict == null)
            {
                return Results.Ok(new { Status = "OK", Conflict = (object?)null });
            }

            var overbookage = conflict.BookedRooms - conflict.AvailableRooms;
            return Results.Ok(new
            {
                Status = "CONFLICT_DETECTED",
                Conflict = new
                {
                    conflict.ConflictId,
                    conflict.PropertyId,
                    conflict.RoomTypeId,
                    conflict.ConflictDate,
                    conflict.BookedRooms,
                    conflict.AvailableRooms,
                    Overbookage = overbookage,
                    conflict.ChannelsInvolved,
                    conflict.ResolutionStrategy
                }
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }

    private static async Task<IResult> PerformFullSync(
        [FromQuery] Guid propertyId,
        [FromQuery] string channel,
        IChannelManager channelManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await channelManager.FullSyncAsync(propertyId, channel, cancellationToken);
            return result
                ? Results.Accepted($"/api/channels/status/{channel}",
                    new { Message = "Full sync initiated", Channel = channel, PropertyId = propertyId })
                : Results.BadRequest(new { Error = "Full sync failed to start" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }
}

// ─── Response DTOs ──────────────────────────────────────────

public record ChannelSyncStatusDto(
    Guid ChannelId,
    DateTime? LastSyncTime,
    int TotalAttempts,
    int SuccessfulSyncs,
    int FailedSyncs,
    double SuccessRate,
    long AverageTimeMs,
    string? LastError);

public record OverbookingConflictDto(
    Guid PropertyId,
    Guid RoomTypeId,
    DateTime ConflictDate,
    int BookedRooms,
    int AvailableRooms,
    int Overbookage,
    DateTime DetectedAt,
    string ResolutionStrategy);
