namespace SAFARIstack.Modules.Events.Contracts;

/// <summary>
/// Domain event contracts published by core PMS
/// These are consumed asynchronously by analytics, channel manager, revenue systems, etc.
/// Maintains loose coupling - modules subscribe without direct database access
/// </summary>

// ===== BOOKING EVENTS =====
public record BookingConfirmedEvent(
    Guid BookingId,
    Guid PropertyId,
    Guid GuestSegmentId,
    decimal TotalRate,
    int NightCount,
    DateTime CheckIn,
    DateTime CheckOut,
    string[] RoomTypes,
    DateTime PublishedAt = default);

public record BookingModifiedEvent(
    Guid BookingId,
    Guid PropertyId,
    decimal OldRate,
    decimal NewRate,
    DateTime OldCheckOut,
    DateTime NewCheckOut,
    DateTime PublishedAt = default);

public record BookingCancelledEvent(
    Guid BookingId,
    Guid PropertyId,
    string CancellationReason,
    DateTime PublishedAt = default);

public record CheckInCompletedEvent(
    Guid BookingId,
    Guid GuestSegmentId,
    DateTime ActualCheckInTime,
    DateTime PublishedAt = default);

public record CheckOutCompletedEvent(
    Guid BookingId,
    Guid GuestSegmentId,
    DateTime ActualCheckOutTime,
    decimal FinalAmount,
    DateTime PublishedAt = default);

// ===== RFID/ATTENDANCE EVENTS =====
public record StaffCheckedInEvent(
    Guid AttendanceId,
    Guid StaffId,
    string CardUid,
    DateTime CheckInTime,
    Guid? ReaderId,
    DateTime PublishedAt = default);

public record StaffCheckedOutEvent(
    Guid AttendanceId,
    Guid StaffId,
    DateTime CheckOutTime,
    decimal HoursWorked,
    decimal WageAmount,
    DateTime PublishedAt = default);

// ===== REVENUE EVENTS =====
public record RateUpdatedEvent(
    Guid PropertyId,
    Guid RoomTypeId,
    DateRange Period,
    decimal OldRate,
    decimal NewRate,
    string UpdatedBy,
    DateTime PublishedAt = default);

public record AvailabilityChangedEvent(
    Guid PropertyId,
    Guid RoomId,
    DateRange AffectedPeriod,
    int OldAvailable,
    int NewAvailable,
    string Reason,
    DateTime PublishedAt = default);

// ===== CHANNEL EVENTS =====
public record OTASyncRequestedEvent(
    Guid PropertyId,
    string Channel, // "booking.com", "expedia", "airbnb"
    string SyncType, // "availability", "rate", "restriction"
    DateTime PublishedAt = default);

public record OTASyncCompletedEvent(
    Guid PropertyId,
    string Channel,
    bool Success,
    int RecordsSync,
    string? ErrorMessage,
    DateTime PublishedAt = default);

// ===== GUEST INTERACTION EVENTS =====
public record GuestServiceRequestedEvent(
    Guid BookingId,
    Guid GuestSegmentId,
    string ServiceType, // "housekeeping", "room_service", "maintenance"
    DateTime PublishedAt = default);

public record GuestFeedbackReceivedEvent(
    Guid BookingId,
    Guid GuestSegmentId,
    int Rating, // 1-5
    string Comment,
    string[] FeedbackCategories,
    DateTime PublishedAt = default);

// ===== ENERGY EVENTS =====
public record EnergyConsumptionRecordedEvent(
    Guid PropertyId,
    Guid RoomId,
    decimal KwhConsumed,
    DateTime RecordingTime,
    DateTime PublishedAt = default);

public record LoadSheddingAlertEvent(
    Guid PropertyId,
    DateTime AlertTime,
    int EstimatedDurationMinutes,
    DateTime PublishedAt = default);

// ===== MAINTENANCE EVENTS =====
public record MaintenanceWorkOrderCreatedEvent(
    Guid WorkOrderId,
    Guid PropertyId,
    Guid RoomId,
    string IssueDescription,
    int PriorityLevel,
    DateTime PublishedAt = default);

public record MaintenanceWorkOrderCompletedEvent(
    Guid WorkOrderId,
    string CompletionNotes,
    DateTime CompletionTime,
    DateTime PublishedAt = default);

// ===== INVENTORY EVENTS =====
public record InventoryLevelLowEvent(
    Guid PropertyId,
    string ItemType, // "linen", "toiletries", "minibar"
    int CurrentLevel,
    int ReorderThreshold,
    DateTime PublishedAt = default);

public record InventoryReorderTriggeredEvent(
    Guid PropertyId,
    string ItemType,
    int QuantityOrdered,
    string SupplierId,
    DateTime PublishedAt = default);

// ===== VALUE OBJECT =====
public class DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}
