using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Shared.ValueObjects;
using System.Security.Claims;

namespace SAFARIstack.API.Endpoints;

public static class BookingOperationsEndpoints
{
    public static void MapBookingOperationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bookings")
            .WithTags("BookingOperations")
            .RequireAuthorization();

        // POST /api/bookings/{id}/no-show — mark booking as no-show
        group.MapPost("/{id:guid}/no-show", async (Guid id, ApplicationDbContext db) =>
        {
            var booking = await db.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null) return Results.NotFound();

            booking.MarkNoShow();

            // Release assigned rooms back to available
            foreach (var br in booking.BookingRooms)
            {
                var room = await db.Rooms.FindAsync(br.RoomId);
                if (room is not null)
                    room.UpdateStatus(RoomStatus.Available);
            }

            // Calculate no-show penalty if cancellation policy exists
            decimal penalty = 0;
            if (booking.RatePlanId.HasValue)
            {
                var ratePlan = await db.RatePlans
                    .Include(rp => rp.CancellationPolicy)
                    .FirstOrDefaultAsync(rp => rp.Id == booking.RatePlanId);

                if (ratePlan?.CancellationPolicy?.NoShowPenaltyPercentage is not null)
                {
                    penalty = booking.TotalAmount * ratePlan.CancellationPolicy.NoShowPenaltyPercentage.Value;
                }
                else if (ratePlan?.CancellationPolicy is not null)
                {
                    penalty = ratePlan.CancellationPolicy.CalculatePenalty(
                        booking.TotalAmount, booking.CheckInDate, DateTime.UtcNow);
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                booking.Id,
                booking.BookingReference,
                Status = "NoShow",
                PenaltyAmount = penalty,
                RoomsReleased = booking.BookingRooms.Count
            });
        }).WithName("MarkBookingNoShow").WithOpenApi();

        // POST /api/bookings/{id}/cancel — cancel a booking with reason
        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelBookingRequest req, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var booking = await db.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null) return Results.NotFound();

            var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
            booking.Cancel(userId, req.Reason);

            // Release rooms
            foreach (var br in booking.BookingRooms)
            {
                var room = await db.Rooms.FindAsync(br.RoomId);
                if (room is not null)
                    room.UpdateStatus(RoomStatus.Available);
            }

            // Calculate penalty
            decimal penalty = 0;
            if (booking.RatePlanId.HasValue)
            {
                var ratePlan = await db.RatePlans
                    .Include(rp => rp.CancellationPolicy)
                    .FirstOrDefaultAsync(rp => rp.Id == booking.RatePlanId);
                if (ratePlan?.CancellationPolicy is not null)
                {
                    penalty = ratePlan.CancellationPolicy.CalculatePenalty(
                        booking.TotalAmount, booking.CheckInDate, DateTime.UtcNow);
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                booking.Id,
                booking.BookingReference,
                Status = "Cancelled",
                req.Reason,
                PenaltyAmount = penalty,
                CancelledBy = userId
            });
        }).WithName("CancelBooking").WithOpenApi();

        // POST /api/bookings/{id}/copy — duplicate booking with new dates
        group.MapPost("/{id:guid}/copy", async (Guid id, CopyBookingRequest req, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var original = await db.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (original is null) return Results.NotFound();

            var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
            var refNum = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

            var copy = Booking.Create(
                original.PropertyId,
                original.GuestId,
                refNum,
                req.CheckInDate,
                req.CheckOutDate,
                original.AdultCount,
                original.ChildCount,
                userId,
                original.RatePlanId,
                original.Source);

            await db.Bookings.AddAsync(copy);
            await db.SaveChangesAsync();

            return Results.Created($"/api/bookings/{copy.Id}", new
            {
                copy.Id,
                copy.BookingReference,
                OriginalBookingId = id,
                copy.CheckInDate,
                copy.CheckOutDate,
                Message = "Booking duplicated — assign rooms and recalculate financials"
            });
        }).WithName("CopyBooking").WithOpenApi();

        // PUT /api/bookings/{id}/rate-override — override booking rate with audit
        group.MapPut("/{id:guid}/rate-override", async (Guid id, RateOverrideRequest req, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var booking = await db.Bookings.FindAsync(id);
            if (booking is null) return Results.NotFound();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var originalAmount = booking.TotalAmount;

            // Override via CalculateFinancials which the entity supports
            var subtotal = Money.FromZAR(req.NewRate * (booking.CheckOutDate - booking.CheckInDate).Days);
            booking.CalculateFinancials(subtotal);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                booking.Id,
                OriginalAmount = originalAmount,
                NewAmount = booking.TotalAmount,
                req.NewRate,
                req.Reason,
                OverriddenBy = userId,
                OverriddenAt = DateTime.UtcNow
            });
        }).WithName("OverrideBookingRate").WithOpenApi();

        // GET /api/bookings/uninvoiced/{pid} — bookings with no invoice
        group.MapGet("/uninvoiced/{propertyId:guid}", async (Guid propertyId, int page, int pageSize, ApplicationDbContext db) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            var invoicedBookingIds = db.Invoices
                .Select(i => i.FolioId);
            var folioBookingIds = db.Folios
                .Where(f => invoicedBookingIds.Contains(f.Id))
                .Select(f => f.BookingId);

            var query = db.Bookings
                .Where(b => b.PropertyId == propertyId
                    && b.Status == BookingStatus.CheckedOut
                    && !folioBookingIds.Contains(b.Id));

            var total = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.CheckOutDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.Id,
                    b.BookingReference,
                    b.GuestId,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.TotalAmount,
                    Status = b.Status.ToString()
                })
                .ToListAsync();

            return Results.Ok(new { Total = total, Page = page, PageSize = pageSize, Items = bookings });
        }).WithName("GetUninvoicedBookings").WithOpenApi();

        // POST /api/quotations — generate a price quote without creating a booking
        group.MapPost("/quotation", async (QuotationRequest req, ApplicationDbContext db) =>
        {
            var nights = (req.CheckOutDate - req.CheckInDate).Days;
            if (nights <= 0)
                return Results.BadRequest(new { Error = "Check-out must be after check-in" });

            // Get room type base price
            var roomType = await db.RoomTypes.FindAsync(req.RoomTypeId);
            if (roomType is null)
                return Results.NotFound(new { Error = "Room type not found" });

            // Check active season for price multiplier
            var season = await db.Seasons
                .Where(s => s.PropertyId == req.PropertyId && s.IsActive
                    && s.StartDate <= req.CheckInDate && s.EndDate >= req.CheckOutDate)
                .OrderByDescending(s => s.Priority)
                .FirstOrDefaultAsync();

            var multiplier = season?.PriceMultiplier ?? 1.0m;
            var effectiveRate = roomType.BasePrice * multiplier;
            var subtotal = effectiveRate * nights;
            var vatRate = 0.15m; // 15% South African VAT
            var vat = subtotal * vatRate;
            var tourismLevy = subtotal * 0.01m; // 1% tourism levy
            var total = subtotal + vat + tourismLevy;

            return Results.Ok(new
            {
                RoomType = roomType.Name,
                NightlyRate = effectiveRate,
                Nights = nights,
                Season = season?.Name ?? "Standard",
                SeasonMultiplier = multiplier,
                Subtotal = subtotal,
                VAT = vat,
                TourismLevy = tourismLevy,
                Total = total,
                Currency = "ZAR",
                ValidUntil = DateTime.UtcNow.AddHours(24),
                Note = "This is a quote only. No booking has been created."
            });
        }).WithName("GenerateQuotation").WithOpenApi();

        // GET /api/bookings/{id}/registration-card — generate registration card data
        group.MapGet("/{id:guid}/registration-card", async (Guid id, ApplicationDbContext db) =>
        {
            var booking = await db.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null) return Results.NotFound();

            var guest = await db.Guests.FindAsync(booking.GuestId);
            var property = await db.Properties.FindAsync(booking.PropertyId);
            var rooms = await db.Rooms
                .Where(r => booking.BookingRooms.Select(br => br.RoomId).Contains(r.Id))
                .Select(r => new { r.RoomNumber, r.Floor })
                .ToListAsync();

            return Results.Ok(new
            {
                Property = new { property?.Name, property?.Address },
                Guest = new
                {
                    guest?.FirstName,
                    guest?.LastName,
                    guest?.Email,
                    guest?.Phone,
                    guest?.IdNumber,
                    IdType = guest?.IdType.ToString(),
                    guest?.Nationality
                },
                Booking = new
                {
                    booking.BookingReference,
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    booking.AdultCount,
                    booking.ChildCount,
                    Nights = (booking.CheckOutDate - booking.CheckInDate).Days,
                    Rooms = rooms,
                    booking.TotalAmount
                },
                GeneratedAt = DateTime.UtcNow
            });
        }).WithName("GetRegistrationCard").WithOpenApi();
    }
}

public record CancelBookingRequest(string Reason);
public record CopyBookingRequest(DateTime CheckInDate, DateTime CheckOutDate);
public record RateOverrideRequest(decimal NewRate, string Reason);
public record QuotationRequest(Guid PropertyId, Guid RoomTypeId, DateTime CheckInDate, DateTime CheckOutDate, int Adults = 2, int Children = 0);
