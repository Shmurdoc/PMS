using Microsoft.AspNetCore.SignalR;
using SAFARIstack.API.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAFARIstack.API.Services;

/// <summary>
/// Helper service to broadcast real-time updates to clients via SignalR.
/// Inject this into endpoints and services to trigger client notifications.
/// 
/// Example:
///   await _signalRNotificationService.BroadcastBookingCreateAsync(propertyId, booking);
/// </summary>
public interface ISignalRNotificationService
{
    // Booking events
    Task BroadcastBookingCreateAsync(Guid propertyId, Guid bookingId, string guestName, int roomNumber, decimal totalPrice, DateTime checkIn, DateTime checkOut);
    Task BroadcastBookingCancelAsync(Guid propertyId, Guid bookingId, string guestName, string reason, decimal refund);
    Task BroadcastCheckInAsync(Guid propertyId, Guid bookingId, Guid guestId, string guestName, int roomNumber, string roomType);
    Task BroadcastCheckOutAsync(Guid propertyId, Guid bookingId, Guid guestId, string guestName, int roomNumber, decimal finalBalance);
    Task BroadcastRoomStatusChangeAsync(Guid propertyId, Guid roomId, int roomNumber, string previousStatus, string newStatus, string? notes = null);

    // Housekeeping events
    Task BroadcastHousekeepingTaskCreateAsync(Guid propertyId, Guid taskId, int roomNumber, string taskType, Guid? assignedToStaffId, string? staffName);
    Task BroadcastTaskStatusChangeAsync(Guid propertyId, Guid taskId, string previousStatus, string newStatus, Guid? staffId = null, string? completionNotes = null);
    Task BroadcastRoomInspectionAsync(Guid propertyId, Guid inspectionId, Guid taskId, int roomNumber, string result, List<string>? issues = null, Guid? staffId = null);

    // Staff events
    Task BroadcastStaffCheckInAsync(Guid propertyId, Guid staffId, string staffName, string? location = null, string? rfidTagId = null);
    Task BroadcastStaffCheckOutAsync(Guid propertyId, Guid staffId, string staffName, string? location = null);
    Task BroadcastStaffShiftChangeAsync(Guid propertyId, Guid staffId, string staffName, string department, DateTime shiftStart, DateTime shiftEnd, string position);
    Task BroadcastRosterUpdateAsync(Guid propertyId, List<Guid> updatedStaffIds, string updateReason);

    // Revenue events
    Task BroadcastPaymentReceivedAsync(Guid propertyId, Guid bookingId, decimal amount, string currency);
    Task BroadcastRefundIssuedAsync(Guid propertyId, Guid bookingId, decimal amount, string currency);

    // Dashboard events
    Task RequestDashboardRefreshAsync(Guid propertyId, string? refreshScope = null);
    Task BroadcastOccupancyUpdateAsync(Guid propertyId, int totalRooms, int occupiedRooms, int vacantRooms, int dirtyRooms, int maintenanceRooms);
    Task BroadcastRevenueUpdateAsync(Guid propertyId, DateTime date, decimal dailyRevenue, decimal adr, decimal revPar, int checkIns, int checkOuts);

    // System events
    Task SendSystemNotificationAsync(Guid propertyId, string type, object data);
}

/// <summary>
/// Implementation using SignalR Hub context
/// </summary>
public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<PmsHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(IHubContext<PmsHub> hubContext, ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BOOKING EVENTS
    // ═══════════════════════════════════════════════════════════════════

    public async Task BroadcastBookingCreateAsync(Guid propertyId, Guid bookingId, string guestName, int roomNumber, decimal totalPrice, DateTime checkIn, DateTime checkOut)
    {
        try
        {
            var message = new BookingUpdateMessage(
                BookingId: bookingId,
                PropertyId: propertyId,
                GuestName: guestName,
                Status: "confirmed",
                CheckInDate: checkIn,
                CheckOutDate: checkOut,
                RoomNumber: roomNumber,
                TotalPrice: totalPrice,
                UpdateReason: "new_booking");

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("BookingUpdate", message);
            _logger.LogInformation("SignalR: Broadcast BookingCreate PropertyId={PropertyId} BookingId={BookingId}", propertyId, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting booking create");
        }
    }

    public async Task BroadcastBookingCancelAsync(Guid propertyId, Guid bookingId, string guestName, string reason, decimal refund)
    {
        try
        {
            var message = new BookingCancellationMessage(
                BookingId: bookingId,
                GuestName: guestName,
                RoomNumber: 0,
                OriginalCheckIn: null,
                CancellationReason: reason,
                RefundAmount: refund);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("BookingCancelled", message);
            _logger.LogInformation("SignalR: Broadcast BookingCancel PropertyId={PropertyId} BookingId={BookingId}", propertyId, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting booking cancellation");
        }
    }

    public async Task BroadcastCheckInAsync(Guid propertyId, Guid bookingId, Guid guestId, string guestName, int roomNumber, string roomType)
    {
        try
        {
            var message = new CheckInEventMessage(
                BookingId: bookingId,
                GuestId: guestId,
                GuestName: guestName,
                RoomNumber: roomNumber,
                RoomType: roomType,
                CheckInTime: DateTime.UtcNow);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("BookingCheckIn", message);
            _logger.LogInformation("SignalR: Broadcast CheckIn PropertyId={PropertyId} GuestName={GuestName}", propertyId, guestName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting check-in");
        }
    }

    public async Task BroadcastCheckOutAsync(Guid propertyId, Guid bookingId, Guid guestId, string guestName, int roomNumber, decimal finalBalance)
    {
        try
        {
            var message = new CheckOutEventMessage(
                BookingId: bookingId,
                GuestId: guestId,
                GuestName: guestName,
                RoomNumber: roomNumber,
                CheckOutTime: DateTime.UtcNow,
                FinalBalance: finalBalance);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("BookingCheckOut", message);
            _logger.LogInformation("SignalR: Broadcast CheckOut PropertyId={PropertyId} GuestName={GuestName}", propertyId, guestName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting check-out");
        }
    }

    public async Task BroadcastRoomStatusChangeAsync(Guid propertyId, Guid roomId, int roomNumber, string previousStatus, string newStatus, string? notes = null)
    {
        try
        {
            var message = new RoomStatusMessage(
                RoomId: roomId,
                RoomNumber: roomNumber,
                PreviousStatus: previousStatus,
                Status: newStatus,
                Notes: notes);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("RoomStatusChanged", message);
            _logger.LogInformation("SignalR: Broadcast RoomStatusChange PropertyId={PropertyId} Room={RoomNumber} NewStatus={NewStatus}", propertyId, roomNumber, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting room status change");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HOUSEKEEPING EVENTS
    // ═══════════════════════════════════════════════════════════════════

    public async Task BroadcastHousekeepingTaskCreateAsync(Guid propertyId, Guid taskId, int roomNumber, string taskType, Guid? assignedToStaffId, string? staffName)
    {
        try
        {
            var message = new HousekeepingTaskMessage(
                TaskId: taskId,
                PropertyId: propertyId,
                RoomNumber: roomNumber,
                TaskType: taskType,
                Status: "assigned",
                AssignedToStaffId: assignedToStaffId,
                StaffName: staffName);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("HousekeepingUpdate", message);
            _logger.LogInformation("SignalR: Broadcast HousekeepingTaskCreate PropertyId={PropertyId} StaffName={StaffName}", propertyId, staffName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting housekeeping task");
        }
    }

    public async Task BroadcastTaskStatusChangeAsync(Guid propertyId, Guid taskId, string previousStatus, string newStatus, Guid? staffId = null, string? completionNotes = null)
    {
        try
        {
            var message = new TaskStatusChangeMessage(
                TaskId: taskId,
                PreviousStatus: previousStatus,
                NewStatus: newStatus,
                StaffId: staffId,
                CompletionNotes: completionNotes,
                CompletedAt: newStatus == "completed" ? DateTime.UtcNow : null);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("HousekeepingTaskStatus", message);
            _logger.LogInformation("SignalR: Broadcast TaskStatusChange PropertyId={PropertyId} NewStatus={NewStatus}", propertyId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting task status change");
        }
    }

    public async Task BroadcastRoomInspectionAsync(Guid propertyId, Guid inspectionId, Guid taskId, int roomNumber, string result, List<string>? issues = null, Guid? staffId = null)
    {
        try
        {
            var message = new RoomInspectionMessage(
                InspectionId: inspectionId,
                TaskId: taskId,
                RoomNumber: roomNumber,
                InspectionResult: result,
                Issues: issues,
                InspectedByStaffId: staffId);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("RoomInspection", message);
            _logger.LogInformation("SignalR: Broadcast RoomInspection PropertyId={PropertyId} Result={Result}", propertyId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting room inspection");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STAFF EVENTS
    // ═══════════════════════════════════════════════════════════════════

    public async Task BroadcastStaffCheckInAsync(Guid propertyId, Guid staffId, string staffName, string? location = null, string? rfidTagId = null)
    {
        try
        {
            var message = new StaffAttendanceMessage(
                StaffId: staffId,
                StaffName: staffName,
                Action: "check_in",
                AttendanceTime: DateTime.UtcNow,
                RfidTagId: rfidTagId,
                Location: location);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("StaffAttendance", message);
            _logger.LogInformation("SignalR: Broadcast StaffCheckIn PropertyId={PropertyId} StaffName={StaffName}", propertyId, staffName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting staff check-in");
        }
    }

    public async Task BroadcastStaffCheckOutAsync(Guid propertyId, Guid staffId, string staffName, string? location = null)
    {
        try
        {
            var message = new StaffAttendanceMessage(
                StaffId: staffId,
                StaffName: staffName,
                Action: "check_out",
                AttendanceTime: DateTime.UtcNow,
                RfidTagId: null,
                Location: location);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("StaffAttendance", message);
            _logger.LogInformation("SignalR: Broadcast StaffCheckOut PropertyId={PropertyId} StaffName={StaffName}", propertyId, staffName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting staff check-out");
        }
    }

    public async Task BroadcastStaffShiftChangeAsync(Guid propertyId, Guid staffId, string staffName, string department, DateTime shiftStart, DateTime shiftEnd, string position)
    {
        try
        {
            var message = new StaffShiftMessage(
                StaffId: staffId,
                StaffName: staffName,
                Department: department,
                ShiftStart: shiftStart,
                ShiftEnd: shiftEnd,
                Position: position);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("StaffUpdate", message);
            _logger.LogInformation("SignalR: Broadcast StaffShiftChange PropertyId={PropertyId} StaffName={StaffName}", propertyId, staffName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting staff shift change");
        }
    }

    public async Task BroadcastRosterUpdateAsync(Guid propertyId, List<Guid> updatedStaffIds, string updateReason)
    {
        try
        {
            var message = new RosterUpdateMessage(
                PropertyId: propertyId,
                EffectiveDate: DateTime.UtcNow.Date,
                TotalStaffScheduled: updatedStaffIds.Count,
                UpdatedStaffIds: updatedStaffIds,
                UpdateReason: updateReason);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("RosterUpdate", message);
            _logger.LogInformation("SignalR: Broadcast RosterUpdate PropertyId={PropertyId} StaffCount={StaffCount}", propertyId, updatedStaffIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting roster update");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  REVENUE EVENTS
    // ═══════════════════════════════════════════════════════════════════

    public async Task BroadcastPaymentReceivedAsync(Guid propertyId, Guid bookingId, decimal amount, string currency)
    {
        try
        {
            var message = new RevenueUpdateMessage(
                BookingId: bookingId,
                PropertyId: propertyId,
                TransactionType: "payment",
                Amount: amount,
                Currency: currency,
                PaymentStatus: "completed",
                TransactionTime: DateTime.UtcNow);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("RevenueUpdate", message);
            _logger.LogInformation("SignalR: Broadcast PaymentReceived PropertyId={PropertyId} Amount={Amount}", propertyId, amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting payment received");
        }
    }

    public async Task BroadcastRefundIssuedAsync(Guid propertyId, Guid bookingId, decimal amount, string currency)
    {
        try
        {
            var message = new RevenueUpdateMessage(
                BookingId: bookingId,
                PropertyId: propertyId,
                TransactionType: "refund",
                Amount: amount,
                Currency: currency,
                PaymentStatus: "refunded",
                TransactionTime: DateTime.UtcNow);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("RevenueUpdate", message);
            _logger.LogInformation("SignalR: Broadcast RefundIssued PropertyId={PropertyId} Amount={Amount}", propertyId, amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting refund issued");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DASHBOARD EVENTS
    // ═══════════════════════════════════════════════════════════════════

    public async Task RequestDashboardRefreshAsync(Guid propertyId, string? refreshScope = null)
    {
        try
        {
            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("DashboardRefresh", new
            {
                PropertyId = propertyId,
                Timestamp = DateTime.UtcNow,
                RefreshScope = refreshScope ?? "full"
            });
            _logger.LogInformation("SignalR: RequestDashboardRefresh PropertyId={PropertyId} Scope={Scope}", propertyId, refreshScope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting dashboard refresh");
        }
    }

    public async Task BroadcastOccupancyUpdateAsync(Guid propertyId, int totalRooms, int occupiedRooms, int vacantRooms, int dirtyRooms, int maintenanceRooms)
    {
        try
        {
            var occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms : 0;
            var message = new OccupancyUpdateMessage(
                PropertyId: propertyId,
                TotalRooms: totalRooms,
                OccupiedRooms: occupiedRooms,
                VacantRooms: vacantRooms,
                DirtyRooms: dirtyRooms,
                MaintenanceRooms: maintenanceRooms,
                OccupancyRate: occupancyRate);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("OccupancyUpdate", message);
            _logger.LogInformation("SignalR: Broadcast OccupancyUpdate PropertyId={PropertyId} Rate={Rate}%", propertyId, occupancyRate * 100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting occupancy update");
        }
    }

    public async Task BroadcastRevenueUpdateAsync(Guid propertyId, DateTime date, decimal dailyRevenue, decimal adr, decimal revPar, int checkIns, int checkOuts)
    {
        try
        {
            var message = new RevenueChartMessage(
                PropertyId: propertyId,
                Date: date,
                DailyRevenue: dailyRevenue,
                AverageDailyRate: adr,
                RevenuePerAvailableRoom: revPar,
                CheckInsToday: checkIns,
                CheckOutsToday: checkOuts);

            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("RevenueChartUpdate", message);
            _logger.LogInformation("SignalR: Broadcast RevenueChartUpdate PropertyId={PropertyId} DailyRevenue={DailyRevenue}", propertyId, dailyRevenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting revenue update");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SYSTEM EVENTS
    // ═══════════════════════════════════════════════════════════════════

    public async Task SendSystemNotificationAsync(Guid propertyId, string type, object data)
    {
        try
        {
            await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("SystemNotification", new
            {
                Type = type,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
            _logger.LogInformation("SignalR: SendSystemNotification PropertyId={PropertyId} Type={Type}", propertyId, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system notification");
        }
    }
}
