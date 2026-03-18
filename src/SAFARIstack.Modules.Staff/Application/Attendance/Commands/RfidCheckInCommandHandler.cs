using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.Modules.Staff.Application.Attendance.Commands;

public class RfidCheckInCommandHandler : IRequestHandler<RfidCheckInCommand, RfidCheckInResult>
{
    private readonly DbContext _context;
    private readonly ILogger<RfidCheckInCommandHandler> _logger;
    private static readonly Dictionary<string, DateTime> _recentCheckIns = new();
    private const int VELOCITY_CHECK_SECONDS = 5; // Prevent duplicate check-ins within 5 seconds

    public RfidCheckInCommandHandler(
        DbContext context,
        ILogger<RfidCheckInCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RfidCheckInResult> Handle(
        RfidCheckInCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Velocity check: prevent rapid duplicate scans
            if (_recentCheckIns.TryGetValue(request.CardUid, out var lastCheckIn))
            {
                if ((DateTime.UtcNow - lastCheckIn).TotalSeconds < VELOCITY_CHECK_SECONDS)
                {
                    _logger.LogWarning("Velocity check failed for card {CardUid}", request.CardUid);
                    return new RfidCheckInResult(
                        false,
                        null,
                        null,
                        null,
                        "Duplicate scan detected. Please wait a few seconds.",
                        "VELOCITY_CHECK_FAILED");
                }
            }

            // Validate reader if ReaderId provided
            if (request.ReaderId.HasValue)
            {
                var reader = await _context.Set<RfidReader>()
                    .FirstOrDefaultAsync(r => r.Id == request.ReaderId.Value, cancellationToken);

                if (reader == null)
                {
                    return new RfidCheckInResult(
                        false,
                        null,
                        null,
                        null,
                        "Reader not found",
                        "READER_NOT_FOUND");
                }

                // Validate API key if provided
                if (!string.IsNullOrEmpty(request.ReaderApiKey) &&
                    !reader.ValidateApiKey(request.ReaderApiKey))
                {
                    _logger.LogWarning("Invalid API key for reader {ReaderId}", request.ReaderId);
                    return new RfidCheckInResult(
                        false,
                        null,
                        null,
                        null,
                        "Invalid reader authentication",
                        "INVALID_API_KEY");
                }

                // Update reader last seen
                reader.UpdateLastSeen();
            }

            // Find RFID card
            var card = await _context.Set<RfidCard>()
                .Include(c => c.StaffMember)
                .FirstOrDefaultAsync(c => c.CardUid == request.CardUid, cancellationToken);

            if (card == null)
            {
                _logger.LogWarning("Card not found: {CardUid}", request.CardUid);
                return new RfidCheckInResult(
                    false,
                    null,
                    null,
                    null,
                    "Card not recognized",
                    "CARD_NOT_FOUND");
            }

            if (card.Status != RfidCardStatus.Active)
            {
                _logger.LogWarning("Inactive card attempted check-in: {CardUid} - Status: {Status}",
                    request.CardUid, card.Status);
                return new RfidCheckInResult(
                    false,
                    null,
                    card.StaffMember.FullName,
                    null,
                    $"Card is {card.Status.ToString().ToLower()}",
                    "CARD_INACTIVE");
            }

            // Check if already checked in today
            var today = DateTime.UtcNow.Date;
            var existingAttendance = await _context.Set<StaffAttendance>()
                .Where(a => a.StaffId == card.StaffId &&
                           a.CheckInTime.Date == today &&
                           !a.CheckOutTime.HasValue)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAttendance != null)
            {
                return new RfidCheckInResult(
                    false,
                    existingAttendance.Id,
                    card.StaffMember.FullName,
                    existingAttendance.CheckInTime,
                    "Already checked in",
                    "ALREADY_CHECKED_IN");
            }

            // Get staff schedule (simplified - you'd query actual schedule)
            var scheduledHours = 8.0m;
            var hourlyRate = card.StaffMember.HourlyRate ?? 150.00m; // Default R150/hour

            // Create attendance record
            var attendance = StaffAttendance.CheckIn(
                card.StaffId,
                card.StaffMember.PropertyId,
                request.CardUid,
                request.ReaderId,
                ShiftType.Morning, // This would come from schedule
                scheduledHours,
                hourlyRate);

            await _context.Set<StaffAttendance>().AddAsync(attendance, cancellationToken);

            // Update card last used
            card.UpdateLastUsed();

            await _context.SaveChangesAsync(cancellationToken);

            // Update velocity check cache
            _recentCheckIns[request.CardUid] = DateTime.UtcNow;

            _logger.LogInformation("Staff {StaffName} checked in with card {CardUid}",
                card.StaffMember.FullName, request.CardUid);

            return new RfidCheckInResult(
                true,
                attendance.Id,
                card.StaffMember.FullName,
                attendance.CheckInTime,
                "Check-in successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RFID check-in for card {CardUid}", request.CardUid);
            return new RfidCheckInResult(
                false,
                null,
                null,
                null,
                "An error occurred during check-in",
                "INTERNAL_ERROR");
        }
    }
}
