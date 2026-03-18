using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Modules.Staff.Domain.Entities;

/// <summary>
/// Staff Attendance record with BCEA (Basic Conditions of Employment Act) compliance
/// </summary>
public class StaffAttendance : AuditableAggregateRoot, IMultiTenant
{
    public Guid StaffId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string CardUid { get; private set; } = string.Empty;

    // Time Tracking
    public DateTime CheckInTime { get; private set; }
    public DateTime? CheckOutTime { get; private set; }

    // Location Tracking
    public Guid? ReaderId { get; private set; }
    public string? LocationType { get; private set; }

    // Shift Details
    public ShiftType ShiftType { get; private set; }
    public decimal ScheduledHours { get; private set; }
    public decimal? ActualHours { get; private set; }

    // Break Tracking
    public DateTime? BreakStart { get; private set; }
    public DateTime? BreakEnd { get; private set; }
    public decimal BreakDuration { get; private set; }

    // SA Labor Compliance
    public decimal OvertimeHours { get; private set; }
    public decimal HourlyRate { get; private set; }
    public decimal OvertimeRate { get; private set; }
    public decimal TotalWage { get; private set; }

    // Status
    public AttendanceStatus Status { get; private set; } = AttendanceStatus.CheckedIn;
    public Guid? VerifiedByUserId { get; private set; }
    public string? VerificationNotes { get; private set; }

    // Geolocation (for mobile check-in/out)
    public decimal? CheckInLatitude { get; private set; }
    public decimal? CheckInLongitude { get; private set; }
    public decimal? CheckOutLatitude { get; private set; }
    public decimal? CheckOutLongitude { get; private set; }

    // Navigation
    public StaffMember StaffMember { get; private set; } = null!;
    public RfidReader? Reader { get; private set; }

    private StaffAttendance() { } // EF Core

    public static StaffAttendance CheckIn(
        Guid staffId,
        Guid propertyId,
        string cardUid,
        Guid? readerId,
        ShiftType shiftType,
        decimal scheduledHours,
        decimal hourlyRate)
    {
        var attendance = new StaffAttendance
        {
            StaffId = staffId,
            PropertyId = propertyId,
            CardUid = cardUid,
            CheckInTime = DateTime.UtcNow,
            ReaderId = readerId,
            ShiftType = shiftType,
            ScheduledHours = scheduledHours,
            HourlyRate = hourlyRate,
            OvertimeRate = hourlyRate * 1.5m, // SA law: 1.5x for overtime
            Status = AttendanceStatus.CheckedIn
        };

        attendance.AddDomainEvent(new StaffCheckedInEvent(attendance.Id, staffId, cardUid));
        return attendance;
    }

    public void CheckOut(Guid? readerId)
    {
        if (CheckOutTime.HasValue)
            throw new InvalidOperationException("Staff member already checked out");

        CheckOutTime = DateTime.UtcNow;
        ReaderId = readerId;
        Status = AttendanceStatus.CheckedOut;
        
        CalculateWorkHours();
        
        AddDomainEvent(new StaffCheckedOutEvent(Id, StaffId, CardUid));
    }

    public void StartBreak()
    {
        if (BreakStart.HasValue)
            throw new InvalidOperationException("Break already started");

        BreakStart = DateTime.UtcNow;
        Status = AttendanceStatus.OnBreak;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EndBreak()
    {
        if (!BreakStart.HasValue)
            throw new InvalidOperationException("No active break to end");

        if (BreakEnd.HasValue)
            throw new InvalidOperationException("Break already ended");

        BreakEnd = DateTime.UtcNow;
        BreakDuration = (decimal)(BreakEnd.Value - BreakStart.Value).TotalHours;
        Status = AttendanceStatus.CheckedIn;
        UpdatedAt = DateTime.UtcNow;
    }

    private void CalculateWorkHours()
    {
        if (!CheckOutTime.HasValue)
            return;

        var totalHours = (decimal)(CheckOutTime.Value - CheckInTime).TotalHours;
        ActualHours = totalHours - BreakDuration;

        // Calculate overtime (SA BCEA: over 9 hours per day or 45 hours per week)
        if (ActualHours > ScheduledHours)
        {
            OvertimeHours = ActualHours.Value - ScheduledHours;
        }

        // Calculate total wage
        var regularWage = Math.Min(ActualHours.Value, ScheduledHours) * HourlyRate;
        var overtimeWage = OvertimeHours * OvertimeRate;
        TotalWage = regularWage + overtimeWage;

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMobileCheckInLocation(decimal latitude, decimal longitude)
    {
        CheckInLatitude = latitude;
        CheckInLongitude = longitude;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMobileCheckOutLocation(decimal latitude, decimal longitude)
    {
        CheckOutLatitude = latitude;
        CheckOutLongitude = longitude;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum AttendanceStatus
{
    CheckedIn,
    OnBreak,
    CheckedOut,
    Missed
}

public enum ShiftType
{
    Morning,
    Afternoon,
    Night,
    Split
}

public record StaffCheckedInEvent(Guid AttendanceId, Guid StaffId, string CardUid) : DomainEvent;
public record StaffCheckedOutEvent(Guid AttendanceId, Guid StaffId, string CardUid) : DomainEvent;
