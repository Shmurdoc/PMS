using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.Modules.Staff.Application.Attendance.Commands;

public class RfidCheckOutCommandHandler : IRequestHandler<RfidCheckOutCommand, RfidCheckOutResult>
{
    private readonly DbContext _context;
    private readonly ILogger<RfidCheckOutCommandHandler> _logger;

    public RfidCheckOutCommandHandler(
        DbContext context,
        ILogger<RfidCheckOutCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RfidCheckOutResult> Handle(
        RfidCheckOutCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate reader if ReaderId provided
            if (request.ReaderId.HasValue)
            {
                var reader = await _context.Set<RfidReader>()
                    .FirstOrDefaultAsync(r => r.Id == request.ReaderId.Value, cancellationToken);

                if (reader == null || !reader.ValidateApiKey(request.ReaderApiKey ?? string.Empty))
                {
                    return new RfidCheckOutResult(
                        false,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        "Invalid reader authentication",
                        "INVALID_READER");
                }

                reader.UpdateLastSeen();
            }

            // Find active attendance for today
            var today = DateTime.UtcNow.Date;
            var attendance = await _context.Set<StaffAttendance>()
                .Include(a => a.StaffMember)
                .Where(a => a.CardUid == request.CardUid &&
                           a.CheckInTime.Date == today &&
                           !a.CheckOutTime.HasValue)
                .FirstOrDefaultAsync(cancellationToken);

            if (attendance == null)
            {
                _logger.LogWarning("No active attendance found for card {CardUid}", request.CardUid);
                return new RfidCheckOutResult(
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "No active check-in found",
                    "NOT_CHECKED_IN");
            }

            // Check out
            attendance.CheckOut(request.ReaderId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Staff {StaffName} checked out with card {CardUid}. Hours: {Hours}, Overtime: {Overtime}",
                attendance.StaffMember.FullName,
                request.CardUid,
                attendance.ActualHours,
                attendance.OvertimeHours);

            return new RfidCheckOutResult(
                true,
                attendance.Id,
                attendance.StaffMember.FullName,
                attendance.CheckOutTime,
                attendance.ActualHours,
                attendance.OvertimeHours,
                attendance.TotalWage,
                "Check-out successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RFID check-out for card {CardUid}", request.CardUid);
            return new RfidCheckOutResult(
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                "An error occurred during check-out",
                "INTERNAL_ERROR");
        }
    }
}
