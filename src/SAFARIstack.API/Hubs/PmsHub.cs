using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SAFARIstack.API.Hubs;

/// <summary>
/// Real-time PMS hub — pushes live updates to all connected clients.
/// Clients (WPF, Blazor WASM, MAUI, PWA) connect via /hubs/pms with JWT bearer token.
/// 
/// Server → Client events:
///   "BookingUpdate"        → Booking created/modified/cancelled
///   "BookingCheckIn"       → Guest checked in
///   "BookingCheckOut"      → Guest checked out
///   "RoomStatusChanged"    → Room status or housekeeping status changed
///   "GuestActivity"        → Check-in, check-out, VIP arrival
///   "HousekeepingUpdate"   → Task assigned/completed/inspected
///   "HousekeepingTaskStatus" → Task status change
///   "RevenueUpdate"        → Payment received/refunded
///   "SystemNotification"   → Alerts, maintenance windows, announcements
///   "StaffUpdate"          → Shift changes, attendance, RFID events
///   "StaffAttendance"      → Staff check-in/out via RFID
///   "DashboardRefresh"     → KPI data changed, refresh dashboard charts
/// </summary>
[Authorize]
public class PmsHub : Hub
{
    private readonly ILogger<PmsHub> _logger;

    public PmsHub(ILogger<PmsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var propertyId = Context.User?.FindFirst("propertyId")?.Value;
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (propertyId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"property-{propertyId}");
            _logger.LogInformation(
                "SignalR: User {UserId} joined property group {PropertyId} (ConnectionId: {ConnId})",
                userId, propertyId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var propertyId = Context.User?.FindFirst("propertyId")?.Value;
        if (propertyId != null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"property-{propertyId}");

        _logger.LogInformation("SignalR: Connection {ConnId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CLIENT-INVOCABLE METHODS (Hub → Caller)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Explicitly join a property's notification group.
    /// </summary>
    public async Task JoinPropertyGroup(string propertyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"property-{propertyId}");
        _logger.LogInformation("SignalR: {ConnId} manually joined property-{PropertyId}", Context.ConnectionId, propertyId);
    }

    /// <summary>
    /// Leave a property's notification group.
    /// </summary>
    public async Task LeavePropertyGroup(string propertyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"property-{propertyId}");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BOOKING BROADCAST METHODS (Server → Clients)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Broadcast new/updated booking to all clients in property.
    /// Called from BookingEndpoints after create/update.
    /// </summary>
    public async Task BroadcastBookingUpdate(Guid propertyId, BookingUpdateMessage booking)
    {
        booking.Timestamp = DateTime.UtcNow;
        booking.BroadcastedBy = Context.User?.FindFirst("sub")?.Value;
        
        await Clients.Group($"property-{propertyId}").SendAsync("BookingUpdate", booking);
        
        _logger.LogInformation(
            "SignalR: Broadcast BookingUpdate PropertyId={PropertyId} BookingId={BookingId} Status={Status}",
            propertyId, booking.BookingId, booking.Status);
    }

    /// <summary>
    /// Broadcast guest check-in event.
    /// Called from BookingEndpoints CheckIn endpoint.
    /// </summary>
    public async Task BroadcastCheckIn(Guid propertyId, CheckInEventMessage checkInEvent)
    {
        checkInEvent.Timestamp = DateTime.UtcNow;
        checkInEvent.EventType = "check_in";
        
        await Clients.Group($"property-{propertyId}").SendAsync("BookingCheckIn", checkInEvent);
        
        _logger.LogInformation(
            "SignalR: Broadcast CheckIn PropertyId={PropertyId} BookingId={BookingId} RoomNumber={RoomNumber}",
            propertyId, checkInEvent.BookingId, checkInEvent.RoomNumber);
    }

    /// <summary>
    /// Broadcast guest check-out event.
    /// Called from BookingEndpoints CheckOut endpoint.
    /// </summary>
    public async Task BroadcastCheckOut(Guid propertyId, CheckOutEventMessage checkOutEvent)
    {
        checkOutEvent.Timestamp = DateTime.UtcNow;
        checkOutEvent.EventType = "check_out";
        
        await Clients.Group($"property-{propertyId}").SendAsync("BookingCheckOut", checkOutEvent);
        
        _logger.LogInformation(
            "SignalR: Broadcast CheckOut PropertyId={PropertyId} BookingId={BookingId}",
            propertyId, checkOutEvent.BookingId);
    }

    /// <summary>
    /// Broadcast room status change (vacant, occupied, dirty, maintenance).
    /// </summary>
    public async Task BroadcastRoomStatusChange(Guid propertyId, RoomStatusMessage roomStatus)
    {
        roomStatus.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("RoomStatusChanged", roomStatus);
        
        _logger.LogInformation(
            "SignalR: Broadcast RoomStatusChange PropertyId={PropertyId} RoomId={RoomId} Status={Status}",
            propertyId, roomStatus.RoomId, roomStatus.Status);
    }

    /// <summary>
    /// Broadcast booking cancellation.
    /// </summary>
    public async Task BroadcastBookingCancellation(Guid propertyId, BookingCancellationMessage cancellation)
    {
        cancellation.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("BookingCancelled", cancellation);
        
        _logger.LogInformation(
            "SignalR: Broadcast BookingCancellation PropertyId={PropertyId} BookingId={BookingId}",
            propertyId, cancellation.BookingId);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HOUSEKEEPING BROADCAST METHODS (Server → Clients)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Broadcast housekeeping task assignment/update.
    /// Called from HousekeepingEndpoints after task creation.
    /// </summary>
    public async Task BroadcastHousekeepingTaskUpdate(Guid propertyId, HousekeepingTaskMessage task)
    {
        task.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("HousekeepingUpdate", task);
        
        _logger.LogInformation(
            "SignalR: Broadcast HousekeepingUpdate PropertyId={PropertyId} TaskId={TaskId} Status={Status}",
            propertyId, task.TaskId, task.Status);
    }

    /// <summary>
    /// Broadcast task status change (assigned, in_progress, completed, inspected).
    /// </summary>
    public async Task BroadcastTaskStatusChange(Guid propertyId, TaskStatusChangeMessage statusChange)
    {
        statusChange.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("HousekeepingTaskStatus", statusChange);
        
        _logger.LogInformation(
            "SignalR: Broadcast TaskStatusChange PropertyId={PropertyId} TaskId={TaskId} NewStatus={NewStatus}",
            propertyId, statusChange.TaskId, statusChange.NewStatus);
    }

    /// <summary>
    /// Broadcast room inspection result.
    /// </summary>
    public async Task BroadcastRoomInspection(Guid propertyId, RoomInspectionMessage inspection)
    {
        inspection.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("RoomInspection", inspection);
        
        _logger.LogInformation(
            "SignalR: Broadcast RoomInspection PropertyId={PropertyId} RoomNumber={RoomNumber} Result={Result}",
            propertyId, inspection.RoomNumber, inspection.InspectionResult);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STAFF BROADCAST METHODS (Server → Clients)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Broadcast staff attendance event (check-in/out via RFID).
    /// Called from StaffEndpoints CheckIn/CheckOut endpoints.
    /// </summary>
    public async Task BroadcastStaffAttendance(Guid propertyId, StaffAttendanceMessage attendance)
    {
        attendance.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("StaffAttendance", attendance);
        
        _logger.LogInformation(
            "SignalR: Broadcast StaffAttendance PropertyId={PropertyId} StaffId={StaffId} Action={Action}",
            propertyId, attendance.StaffId, attendance.Action);
    }

    /// <summary>
    /// Broadcast staff shift change.
    /// </summary>
    public async Task BroadcastStaffShiftChange(Guid propertyId, StaffShiftMessage shiftChange)
    {
        shiftChange.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("StaffUpdate", shiftChange);
        
        _logger.LogInformation(
            "SignalR: Broadcast StaffShiftChange PropertyId={PropertyId} StaffId={StaffId}",
            propertyId, shiftChange.StaffId);
    }

    /// <summary>
    /// Broadcast staff roster update (bulk update for shift planning).
    /// </summary>
    public async Task BroadcastRosterUpdate(Guid propertyId, RosterUpdateMessage roster)
    {
        roster.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("RosterUpdate", roster);
        
        _logger.LogInformation(
            "SignalR: Broadcast RosterUpdate PropertyId={PropertyId} UpdatedStaffCount={UpdatedStaffCount}",
            propertyId, roster.UpdatedStaffIds?.Count ?? 0);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SYSTEM & DASHBOARD BROADCAST METHODS (Server → Clients)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Broadcast system notification (alerts, maintenance, announcements).
    /// Can be invoked by admin or scheduled jobs.
    /// </summary>
    public async Task SendNotification(string propertyId, string type, object data)
    {
        await Clients.Group($"property-{propertyId}").SendAsync("SystemNotification", new
        {
            Type = type,
            Data = data,
            Timestamp = DateTime.UtcNow,
            SenderId = Context.User?.FindFirst("sub")?.Value
        });
    }

    /// <summary>
    /// Broadcast revenue update (payment received, refund issued).
    /// </summary>
    public async Task BroadcastRevenueUpdate(Guid propertyId, RevenueUpdateMessage revenue)
    {
        revenue.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("RevenueUpdate", revenue);
        
        _logger.LogInformation(
            "SignalR: Broadcast RevenueUpdate PropertyId={PropertyId} BookingId={BookingId} Amount={Amount}",
            propertyId, revenue.BookingId, revenue.Amount);
    }

    /// <summary>
    /// Request a dashboard refresh for all clients viewing a property.
    /// Triggered when significant KPI data changes (occupancy, revenue, etc.).
    /// </summary>
    public async Task RequestDashboardRefresh(string propertyId, string? refreshScope = null)
    {
        await Clients.OthersInGroup($"property-{propertyId}").SendAsync("DashboardRefresh", new
        {
            PropertyId = propertyId,
            Timestamp = DateTime.UtcNow,
            RefreshScope = refreshScope ?? "full" // "full", "occupancy", "revenue", "staff", "housekeeping"
        });
    }

    /// <summary>
    /// Broadcast occupancy chart data update.
    /// </summary>
    public async Task BroadcastOccupancyUpdate(Guid propertyId, OccupancyUpdateMessage occupancy)
    {
        occupancy.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("OccupancyUpdate", occupancy);
    }

    /// <summary>
    /// Broadcast revenue chart data update.
    /// </summary>
    public async Task BroadcastRevenueChartUpdate(Guid propertyId, RevenueChartMessage revenueChart)
    {
        revenueChart.Timestamp = DateTime.UtcNow;
        
        await Clients.Group($"property-{propertyId}").SendAsync("RevenueChartUpdate", revenueChart);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  MESSAGE TYPES FOR TYPE-SAFE SIGNALR BROADCASTS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Booking created/updated notification
/// </summary>
public record BookingUpdateMessage(
    Guid BookingId,
    Guid PropertyId,
    string? GuestName,
    string Status,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int RoomNumber,
    decimal TotalPrice,
    string? UpdateReason = null)
{
    public DateTime Timestamp { get; set; }
    public string? BroadcastedBy { get; set; }
}

/// <summary>
/// Guest check-in event
/// </summary>
public record CheckInEventMessage(
    Guid BookingId,
    Guid GuestId,
    string GuestName,
    int RoomNumber,
    string RoomType,
    DateTime CheckInTime,
    bool IsVip = false)
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = "check_in";
}

/// <summary>
/// Guest check-out event
/// </summary>
public record CheckOutEventMessage(
    Guid BookingId,
    Guid GuestId,
    string GuestName,
    int RoomNumber,
    DateTime CheckOutTime,
    decimal FinalBalance)
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = "check_out";
}

/// <summary>
/// Room status change (vacant, occupied, dirty, maintenance, inspected)
/// </summary>
public record RoomStatusMessage(
    Guid RoomId,
    int RoomNumber,
    string PreviousStatus,
    string Status,
    string? Notes = null,
    Guid? AssignedToStaffId = null)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Booking cancellation notification
/// </summary>
public record BookingCancellationMessage(
    Guid BookingId,
    string GuestName,
    int RoomNumber,
    DateTime? OriginalCheckIn,
    string CancellationReason,
    decimal RefundAmount)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Housekeeping task created/updated notification
/// </summary>
public record HousekeepingTaskMessage(
    Guid TaskId,
    Guid PropertyId,
    int RoomNumber,
    string TaskType,
    string Status,
    Guid? AssignedToStaffId = null,
    string? StaffName = null,
    int? EstimatedMinutes = null,
    string? Notes = null)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Housekeeping task status change (assigned → in_progress → completed → inspected)
/// </summary>
public record TaskStatusChangeMessage(
    Guid TaskId,
    string PreviousStatus,
    string NewStatus,
    Guid? StaffId = null,
    string? CompletionNotes = null,
    DateTime? CompletedAt = null)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Room inspection result notification
/// </summary>
public record RoomInspectionMessage(
    Guid InspectionId,
    Guid TaskId,
    int RoomNumber,
    string InspectionResult,
    List<string>? Issues = null,
    Guid? InspectedByStaffId = null,
    string? Comments = null)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Staff attendance event (RFID check-in/out)
/// </summary>
public record StaffAttendanceMessage(
    Guid StaffId,
    string StaffName,
    string Action,
    DateTime AttendanceTime,
    string? RfidTagId = null,
    string? Location = null)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Staff shift change notification
/// </summary>
public record StaffShiftMessage(
    Guid StaffId,
    string StaffName,
    string Department,
    DateTime ShiftStart,
    DateTime ShiftEnd,
    string Position,
    string? PreviousShift = null)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Staff roster update (for shift planning views)
/// </summary>
public record RosterUpdateMessage(
    Guid PropertyId,
    DateTime EffectiveDate,
    int TotalStaffScheduled,
    List<Guid>? UpdatedStaffIds = null,
    string? UpdateReason = null)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Revenue/Payment update notification
/// </summary>
public record RevenueUpdateMessage(
    Guid BookingId,
    Guid PropertyId,
    string TransactionType,
    decimal Amount,
    string Currency,
    string PaymentStatus,
    DateTime TransactionTime)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Occupancy chart data update
/// </summary>
public record OccupancyUpdateMessage(
    Guid PropertyId,
    int TotalRooms,
    int OccupiedRooms,
    int VacantRooms,
    int DirtyRooms,
    int MaintenanceRooms,
    decimal OccupancyRate)
{
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Revenue chart data update (ADR, RevPAR, daily revenue)
/// </summary>
public record RevenueChartMessage(
    Guid PropertyId,
    DateTime Date,
    decimal DailyRevenue,
    decimal AverageDailyRate,
    decimal RevenuePerAvailableRoom,
    int CheckInsToday,
    int CheckOutsToday)
{
    public DateTime Timestamp { get; set; }
}
