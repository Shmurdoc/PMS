using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class FinancialOperationsEndpoints
{
    public static void MapFinancialOperationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/financial")
            .WithTags("FinancialOperations")
            .RequireAuthorization();

        // GET /api/financial/debtors-aging/{pid} — invoices grouped by aging bucket
        group.MapGet("/debtors-aging/{propertyId:guid}", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var overdueInvoices = await db.Invoices
                .Where(i => i.PropertyId == propertyId
                    && i.Status != InvoiceStatus.Paid
                    && i.Status != InvoiceStatus.Voided
                    && i.DueDate < now)
                .Select(i => new
                {
                    i.Id,
                    i.InvoiceNumber,
                    i.GuestId,
                    i.DueDate,
                    i.TotalAmount,
                    i.PaidAmount,
                    Outstanding = i.TotalAmount - i.PaidAmount,
                    DaysOverdue = (int)(now - i.DueDate).TotalDays
                })
                .ToListAsync();

            var current = overdueInvoices.Where(i => i.DaysOverdue <= 30).ToList();
            var thirtyDay = overdueInvoices.Where(i => i.DaysOverdue > 30 && i.DaysOverdue <= 60).ToList();
            var sixtyDay = overdueInvoices.Where(i => i.DaysOverdue > 60 && i.DaysOverdue <= 90).ToList();
            var ninetyDay = overdueInvoices.Where(i => i.DaysOverdue > 90 && i.DaysOverdue <= 120).ToList();
            var overOneTwenty = overdueInvoices.Where(i => i.DaysOverdue > 120).ToList();

            return Results.Ok(new
            {
                Summary = new
                {
                    TotalOutstanding = overdueInvoices.Sum(i => i.Outstanding),
                    InvoiceCount = overdueInvoices.Count
                },
                Buckets = new[]
                {
                    new { Label = "Current (0-30 days)", Count = current.Count, Total = current.Sum(i => i.Outstanding), Invoices = current },
                    new { Label = "31-60 days", Count = thirtyDay.Count, Total = thirtyDay.Sum(i => i.Outstanding), Invoices = thirtyDay },
                    new { Label = "61-90 days", Count = sixtyDay.Count, Total = sixtyDay.Sum(i => i.Outstanding), Invoices = sixtyDay },
                    new { Label = "91-120 days", Count = ninetyDay.Count, Total = ninetyDay.Sum(i => i.Outstanding), Invoices = ninetyDay },
                    new { Label = "120+ days", Count = overOneTwenty.Count, Total = overOneTwenty.Sum(i => i.Outstanding), Invoices = overOneTwenty }
                }
            });
        }).WithName("GetDebtorsAging").WithOpenApi();

        // POST /api/financial/refund — process a refund against a payment
        group.MapPost("/refund", async (RefundRequest req, ApplicationDbContext db) =>
        {
            var payment = await db.Payments.FindAsync(req.PaymentId);
            if (payment is null) return Results.NotFound(new { Error = "Payment not found" });

            if (req.Amount > payment.Amount)
                return Results.BadRequest(new { Error = "Refund amount exceeds original payment" });

            var folio = await db.Folios.FindAsync(payment.FolioId);
            if (folio is null) return Results.NotFound(new { Error = "Associated folio not found" });

            // Use the proper CreateRefund factory method
            var refund = Payment.CreateRefund(
                payment.PropertyId,
                payment.FolioId,
                req.Amount,
                payment.Method,
                payment.Id,
                req.Reason);

            folio.RecordPayment(refund);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                RefundId = refund.Id,
                OriginalPaymentId = req.PaymentId,
                req.Amount,
                req.Reason,
                Message = "Refund processed successfully"
            });
        }).WithName("ProcessRefund").WithOpenApi();

        // GET /api/financial/revenue-summary/{pid} — comprehensive revenue summary
        group.MapGet("/revenue-summary/{propertyId:guid}", async (Guid propertyId, DateTime? from, DateTime? to, ApplicationDbContext db) =>
        {
            var startDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var endDate = to ?? DateTime.UtcNow;

            var payments = await db.Payments
                .Where(p => p.PropertyId == propertyId
                    && p.CreatedAt >= startDate && p.CreatedAt <= endDate
                    && p.Amount > 0)
                .ToListAsync();

            var totalRevenue = payments.Sum(p => p.Amount);
            var paymentsByMethod = payments
                .GroupBy(p => p.Method.ToString())
                .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount), Count = g.Count() })
                .ToList();

            // Get refunds in same period
            var refunds = await db.Payments
                .Where(p => p.PropertyId == propertyId
                    && p.CreatedAt >= startDate && p.CreatedAt <= endDate
                    && p.Amount < 0)
                .SumAsync(p => p.Amount);

            var totalBookings = await db.Bookings
                .CountAsync(b => b.PropertyId == propertyId
                    && b.CreatedAt >= startDate && b.CreatedAt <= endDate);

            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId);
            var nights = (endDate - startDate).Days;
            var revPar = totalRooms > 0 && nights > 0 ? totalRevenue / (totalRooms * nights) : 0;
            var adr = totalBookings > 0 ? totalRevenue / totalBookings : 0;

            return Results.Ok(new
            {
                Period = new { From = startDate, To = endDate },
                TotalRevenue = totalRevenue,
                TotalRefunds = Math.Abs(refunds),
                NetRevenue = totalRevenue + refunds,
                TotalBookings = totalBookings,
                ADR = Math.Round(adr, 2),
                RevPAR = Math.Round(revPar, 2),
                ByPaymentMethod = paymentsByMethod
            });
        }).WithName("GetRevenueSummary").WithOpenApi();

        // POST /api/day-end/{pid}/close — day-end close process
        group.MapPost("/day-end/{propertyId:guid}/close", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;

            // Count check-ins/outs today
            var checkIns = await db.Bookings
                .CountAsync(b => b.PropertyId == propertyId
                    && b.Status == BookingStatus.CheckedIn
                    && b.CheckInDate.Date == today);

            var checkOuts = await db.Bookings
                .CountAsync(b => b.PropertyId == propertyId
                    && b.Status == BookingStatus.CheckedOut
                    && b.CheckOutDate.Date == today);

            // Find overdue checkouts
            var overdueCheckouts = await db.Bookings
                .Where(b => b.PropertyId == propertyId
                    && b.Status == BookingStatus.CheckedIn
                    && b.CheckOutDate.Date <= today)
                .Select(b => new { b.Id, b.BookingReference, b.GuestId, b.CheckOutDate })
                .ToListAsync();

            // Revenue for today
            var todayRevenue = await db.Payments
                .Where(p => p.PropertyId == propertyId
                    && p.CreatedAt.Date == today && p.Amount > 0)
                .SumAsync(p => p.Amount);

            // Open folios
            var openFolios = await db.Folios
                .CountAsync(f => f.PropertyId == propertyId
                    && f.Status == FolioStatus.Open);

            // Occupancy
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.Status != RoomStatus.OutOfService);
            var occupiedRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.Status == RoomStatus.Occupied);
            var occupancyPct = totalRooms > 0 ? Math.Round((decimal)occupiedRooms / totalRooms * 100, 1) : 0;

            return Results.Ok(new
            {
                Date = today,
                PropertyId = propertyId,
                CheckInsToday = checkIns,
                CheckOutsToday = checkOuts,
                OverdueCheckouts = overdueCheckouts,
                Revenue = todayRevenue,
                OpenFolios = openFolios,
                Occupancy = new
                {
                    TotalRooms = totalRooms,
                    Occupied = occupiedRooms,
                    OccupancyPercentage = occupancyPct
                },
                ClosedAt = DateTime.UtcNow,
                Status = "Day-end close completed"
            });
        }).WithName("DayEndClose").WithOpenApi();
    }
}

public record RefundRequest(Guid PaymentId, decimal Amount, string Reason);
