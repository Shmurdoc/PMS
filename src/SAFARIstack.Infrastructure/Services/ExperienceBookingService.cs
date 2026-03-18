using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Experience Booking Engine — Safari, activities, dining, wellness management
/// </summary>
public class ExperienceBookingService : IExperienceBookingService
{
    private readonly ApplicationDbContext _db;

    public ExperienceBookingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ExperienceDto>> GetAvailableExperiencesAsync(
        Guid propertyId, DateTime date, int participants)
    {
        var dayOfWeek = (int)date.DayOfWeek;

        var experiences = await _db.Set<Experience>()
            .Include(e => e.Schedules)
            .Where(e => e.PropertyId == propertyId && e.IsActive && e.MaxGuests >= participants)
            .AsNoTracking()
            .ToListAsync();

        // Filter by schedule availability and calculate remaining slots
        var result = new List<ExperienceDto>();
        foreach (var exp in experiences)
        {
            var activeSchedules = exp.Schedules
                .Where(s => s.IsActive && s.DaysOfWeek.Contains(dayOfWeek))
                .ToList();

            if (!activeSchedules.Any()) continue;

            // Count existing bookings for this date
            var bookedCount = await _db.Set<ExperienceBooking>()
                .Where(eb => eb.ExperienceId == exp.Id
                    && eb.ScheduledDate.Date == date.Date
                    && eb.Status != ExperienceBookingStatus.Cancelled
                    && eb.Status != ExperienceBookingStatus.NoShow)
                .SumAsync(eb => eb.ParticipantCount);

            var totalCapacity = activeSchedules.Sum(s => s.MaxCapacity);
            var availableSlots = totalCapacity - bookedCount;

            if (availableSlots >= participants)
            {
                result.Add(new ExperienceDto(
                    exp.Id, exp.Name, exp.Description,
                    exp.Category.ToString(), exp.DurationMinutes,
                    exp.BasePrice, exp.PricePerPerson,
                    availableSlots, exp.ImageUrl,
                    exp.DifficultyLevel.ToString(), exp.Location));
            }
        }

        return result;
    }

    public async Task<ExperienceBookingResultDto> BookExperienceAsync(BookExperienceRequestDto request)
    {
        try
        {
            var experience = await _db.Set<Experience>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.ExperienceId)
                ?? throw new InvalidOperationException($"Experience {request.ExperienceId} not found.");

            if (!experience.IsActive)
                throw new InvalidOperationException("Experience is not available.");

            var totalPrice = experience.PricePerPerson
                ? experience.BasePrice * request.ParticipantCount
                : experience.BasePrice;

            decimal? commissionAmount = null;
            decimal? commissionRate = null;
            if (experience.IsThirdParty && experience.CommissionRate.HasValue)
            {
                commissionRate = experience.CommissionRate;
                commissionAmount = totalPrice * experience.CommissionRate.Value;
            }

            var booking = ExperienceBooking.Create(
                request.PropertyId, request.GuestId, request.ExperienceId,
                request.ScheduledDate, request.ScheduledTime,
                request.ParticipantCount, totalPrice,
                request.BookingId, request.ScheduleId,
                request.SpecialRequests,
                commissionAmount, commissionRate);

            await _db.Set<ExperienceBooking>().AddAsync(booking);
            await _db.SaveChangesAsync();

            return new ExperienceBookingResultDto(true, booking.Id, totalPrice, null);
        }
        catch (Exception ex)
        {
            return new ExperienceBookingResultDto(false, null, 0, ex.Message);
        }
    }

    public async Task<ExperienceBookingResultDto> CancelExperienceBookingAsync(
        Guid experienceBookingId, string reason)
    {
        try
        {
            var booking = await _db.Set<ExperienceBooking>().FindAsync(experienceBookingId)
                ?? throw new InvalidOperationException($"Experience booking {experienceBookingId} not found.");

            booking.Cancel(reason);
            _db.Set<ExperienceBooking>().Update(booking);
            await _db.SaveChangesAsync();

            return new ExperienceBookingResultDto(true, booking.Id, booking.TotalPrice, null);
        }
        catch (Exception ex)
        {
            return new ExperienceBookingResultDto(false, null, 0, ex.Message);
        }
    }

    public async Task RecordFeedbackAsync(Guid experienceBookingId, int score, string? notes)
    {
        var booking = await _db.Set<ExperienceBooking>().FindAsync(experienceBookingId)
            ?? throw new InvalidOperationException($"Experience booking {experienceBookingId} not found.");

        booking.AddFeedback(score, notes);
        _db.Set<ExperienceBooking>().Update(booking);
        await _db.SaveChangesAsync();
    }

    public async Task<ExperienceAnalyticsDto> GetExperienceAnalyticsAsync(
        Guid propertyId, DateTime startDate, DateTime endDate)
    {
        var bookings = await _db.Set<ExperienceBooking>()
            .Include(eb => eb.Experience)
            .Where(eb => eb.PropertyId == propertyId
                && eb.ScheduledDate >= startDate
                && eb.ScheduledDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var totalBookings = bookings.Count;
        var totalRevenue = bookings.Sum(b => b.TotalPrice);
        var avgRating = bookings.Where(b => b.FeedbackScore.HasValue)
            .Select(b => b.FeedbackScore!.Value)
            .DefaultIfEmpty(0)
            .Average();
        var commissionPaid = bookings.Sum(b => b.CommissionAmount ?? 0);

        var byExperience = bookings
            .GroupBy(b => b.ExperienceId)
            .Select(g => new ExperiencePerformanceDto(
                g.Key,
                g.First().Experience.Name,
                g.First().Experience.Category.ToString(),
                g.Count(),
                g.Sum(b => b.TotalPrice),
                g.Where(b => b.FeedbackScore.HasValue)
                    .Select(b => (decimal)b.FeedbackScore!.Value)
                    .DefaultIfEmpty(0)
                    .Average()))
            .ToList();

        return new ExperienceAnalyticsDto(
            totalBookings, totalRevenue,
            (decimal)avgRating, commissionPaid, byExperience);
    }
}
