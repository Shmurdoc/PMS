using MediatR;

namespace SAFARIstack.Modules.Staff.Application.Attendance.Commands;

/// <summary>
/// RFID check-out command (triggered by RFID reader)
/// </summary>
public record RfidCheckOutCommand(
    string CardUid,
    Guid? ReaderId,
    string? ReaderApiKey) : IRequest<RfidCheckOutResult>;

public record RfidCheckOutResult(
    bool Success,
    Guid? AttendanceId,
    string? StaffName,
    DateTime? CheckOutTime,
    decimal? TotalHours,
    decimal? OvertimeHours,
    decimal? TotalWage,
    string? Message,
    string? ErrorCode = null);
