using MediatR;

namespace SAFARIstack.Modules.Staff.Application.Attendance.Commands;

/// <summary>
/// RFID check-in command (triggered by RFID reader)
/// </summary>
public record RfidCheckInCommand(
    string CardUid,
    Guid? ReaderId,
    string? ReaderApiKey) : IRequest<RfidCheckInResult>;

public record RfidCheckInResult(
    bool Success,
    Guid? AttendanceId,
    string? StaffName,
    DateTime? CheckInTime,
    string? Message,
    string? ErrorCode = null);
