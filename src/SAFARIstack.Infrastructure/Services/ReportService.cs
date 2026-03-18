using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Report Generation Engine — PDF/Excel/CSV for all operational reports.
/// Uses lightweight table-based generation. PDF via simple HTML→byte[],
/// Excel via CSV with proper formatting.
/// 
/// Architecture note: QuestPDF or similar library should be added for
/// production-quality PDF. For now, provides structured data output
/// that can be consumed by any PDF renderer.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateDailyOperationsReportAsync(
        Guid propertyId, DateTime date, ReportFormat format)
    {
        var arrivals = await _db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate.Date == date.Date)
            .Include(b => b.Guest)
            .AsNoTracking()
            .ToListAsync();

        var departures = await _db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckOutDate.Date == date.Date)
            .Include(b => b.Guest)
            .AsNoTracking()
            .ToListAsync();

        var inHouse = await _db.Bookings
            .Where(b => b.PropertyId == propertyId
                && b.CheckInDate.Date <= date.Date
                && b.CheckOutDate.Date > date.Date
                && (b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed))
            .AsNoTracking()
            .CountAsync();

        var totalRooms = await _db.Rooms
            .Where(r => r.PropertyId == propertyId)
            .CountAsync();

        var housekeepingTasks = await _db.HousekeepingTasks
            .Where(t => t.PropertyId == propertyId && t.ScheduledDate.Date == date.Date)
            .AsNoTracking()
            .ToListAsync();

        var property = await _db.Properties.FindAsync(propertyId);

        var data = new ReportData
        {
            Title = "Daily Operations Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{date:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Summary", new List<ReportRow>
                {
                    new("Total Rooms", totalRooms.ToString()),
                    new("In-House Guests", inHouse.ToString()),
                    new("Occupancy", $"{(totalRooms > 0 ? (decimal)inHouse / totalRooms * 100 : 0):F1}%"),
                    new("Arrivals Today", arrivals.Count.ToString()),
                    new("Departures Today", departures.Count.ToString()),
                }),
                new("Arrivals", arrivals.Select(a => new ReportRow(
                    a.BookingReference,
                    $"{a.Guest?.FirstName} {a.Guest?.LastName} | {a.Nights}N | {a.TotalAmount:C}"
                )).ToList()),
                new("Departures", departures.Select(d => new ReportRow(
                    d.BookingReference,
                    $"{d.Guest?.FirstName} {d.Guest?.LastName} | Outstanding: {d.OutstandingAmount:C}"
                )).ToList()),
                new("Housekeeping", new List<ReportRow>
                {
                    new("Pending", housekeepingTasks.Count(t => t.Status == HousekeepingTaskStatus.Pending).ToString()),
                    new("In Progress", housekeepingTasks.Count(t => t.Status == HousekeepingTaskStatus.InProgress).ToString()),
                    new("Completed", housekeepingTasks.Count(t => t.Status == HousekeepingTaskStatus.Completed).ToString()),
                }),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateMonthlyFinancialReportAsync(
        Guid propertyId, int year, int month, ReportFormat format)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var payments = await _db.Payments
            .Where(p => p.PropertyId == propertyId && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var invoices = await _db.Invoices
            .Where(i => i.PropertyId == propertyId && i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var bookings = await _db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= startDate && b.CheckInDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var property = await _db.Properties.FindAsync(propertyId);

        var totalRevenue = payments.Sum(p => p.Amount);
        var totalVAT = bookings.Sum(b => b.VATAmount);
        var totalLevy = bookings.Sum(b => b.TourismLevyAmount);

        var paymentsByMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new ReportRow(g.Key.ToString(), $"{g.Sum(p => p.Amount):C} ({g.Count()} transactions)"))
            .ToList();

        var data = new ReportData
        {
            Title = "Monthly Financial Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM}",
            Sections = new List<ReportSection>
            {
                new("Revenue Summary", new List<ReportRow>
                {
                    new("Total Revenue", $"{totalRevenue:C}"),
                    new("Total VAT Collected", $"{totalVAT:C}"),
                    new("Tourism Levy", $"{totalLevy:C}"),
                    new("Net Revenue", $"{totalRevenue - totalVAT - totalLevy:C}"),
                    new("Total Bookings", bookings.Count.ToString()),
                    new("Average Booking Value", $"{(bookings.Count > 0 ? bookings.Average(b => b.TotalAmount) : 0):C}"),
                    new("Invoices Issued", invoices.Count.ToString()),
                }),
                new("Revenue by Payment Method", paymentsByMethod),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateOccupancyReportAsync(
        Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format)
    {
        var totalRooms = await _db.Rooms
            .Where(r => r.PropertyId == propertyId)
            .CountAsync();

        var days = (endDate - startDate).Days;
        var dailyOccupancy = new List<ReportRow>();

        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            var occupied = await _db.Bookings
                .Where(b => b.PropertyId == propertyId
                    && b.CheckInDate.Date <= date.Date
                    && b.CheckOutDate.Date > date.Date
                    && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.NoShow)
                .CountAsync();

            var pct = totalRooms > 0 ? (decimal)occupied / totalRooms * 100 : 0;
            dailyOccupancy.Add(new ReportRow($"{date:yyyy-MM-dd}", $"{occupied}/{totalRooms} ({pct:F1}%)"));
        }

        var avgOccupied = dailyOccupancy.Count > 0
            ? dailyOccupancy.Average(d =>
            {
                var parts = d.Value.Split('/');
                return int.TryParse(parts[0], out var o) ? o : 0;
            }) : 0;

        var property = await _db.Properties.FindAsync(propertyId);

        var data = new ReportData
        {
            Title = "Occupancy Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Summary", new List<ReportRow>
                {
                    new("Total Rooms", totalRooms.ToString()),
                    new("Period (Days)", days.ToString()),
                    new("Average Occupancy", $"{(totalRooms > 0 ? avgOccupied / totalRooms * 100 : 0):F1}%"),
                }),
                new("Daily Breakdown", dailyOccupancy),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateRevenueReportAsync(
        Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format)
    {
        var payments = await _db.Payments
            .Where(p => p.PropertyId == propertyId && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var bookings = await _db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= startDate && b.CheckInDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var upsellRevenue = await _db.Set<UpsellTransaction>()
            .Include(t => t.Offer)
            .Where(t => t.Offer.PropertyId == propertyId
                && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .SumAsync(t => t.UnitPrice * t.Quantity);

        var property = await _db.Properties.FindAsync(propertyId);
        var totalRooms = await _db.Rooms.Where(r => r.PropertyId == propertyId).CountAsync();
        var days = Math.Max((endDate - startDate).Days, 1);
        var totalRoomNights = totalRooms * days;
        var totalRevenue = payments.Sum(p => p.Amount);

        var data = new ReportData
        {
            Title = "Revenue Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Revenue KPIs", new List<ReportRow>
                {
                    new("Total Revenue", $"{totalRevenue:C}"),
                    new("Room Revenue", $"{bookings.Sum(b => b.SubtotalAmount):C}"),
                    new("Upsell Revenue", $"{upsellRevenue:C}"),
                    new("ADR", $"{(bookings.Count > 0 ? bookings.Average(b => b.TotalAmount / Math.Max(b.Nights, 1)) : 0):C}"),
                    new("RevPAR", $"{(totalRoomNights > 0 ? totalRevenue / totalRoomNights : 0):C}"),
                    new("Total Bookings", bookings.Count.ToString()),
                }),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateGuestAnalyticsReportAsync(
        Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format)
    {
        var guests = await _db.Guests
            .Where(g => g.PropertyId == propertyId)
            .AsNoTracking()
            .ToListAsync();

        var bookings = await _db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= startDate && b.CheckInDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var totalGuests = bookings.Select(b => b.GuestId).Distinct().Count();
        var returningGuests = bookings
            .GroupBy(b => b.GuestId)
            .Count(g => g.Count() > 1);

        var bySource = bookings
            .GroupBy(b => b.Source)
            .Select(g => new ReportRow(g.Key.ToString(), $"{g.Count()} bookings ({g.Sum(b => b.TotalAmount):C})"))
            .ToList();

        var property = await _db.Properties.FindAsync(propertyId);

        var data = new ReportData
        {
            Title = "Guest Analytics Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Guest Summary", new List<ReportRow>
                {
                    new("Total Unique Guests", totalGuests.ToString()),
                    new("Returning Guests", returningGuests.ToString()),
                    new("Return Rate", $"{(totalGuests > 0 ? (decimal)returningGuests / totalGuests * 100 : 0):F1}%"),
                    new("Total Guest Database", guests.Count.ToString()),
                }),
                new("Bookings by Source", bySource),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateHousekeepingReportAsync(
        Guid propertyId, DateTime date, ReportFormat format)
    {
        var tasks = await _db.HousekeepingTasks
            .Where(t => t.PropertyId == propertyId && t.ScheduledDate.Date == date.Date)
            .AsNoTracking()
            .ToListAsync();

        var property = await _db.Properties.FindAsync(propertyId);

        var byStatus = tasks
            .GroupBy(t => t.Status)
            .Select(g => new ReportRow(g.Key.ToString(), g.Count().ToString()))
            .ToList();

        var data = new ReportData
        {
            Title = "Housekeeping Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{date:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Task Summary", new List<ReportRow>
                {
                    new("Total Tasks", tasks.Count.ToString()),
                    new("Completed", tasks.Count(t => t.Status == HousekeepingTaskStatus.Completed).ToString()),
                    new("Pending", tasks.Count(t => t.Status == HousekeepingTaskStatus.Pending).ToString()),
                    new("In Progress", tasks.Count(t => t.Status == HousekeepingTaskStatus.InProgress).ToString()),
                }),
                new("By Status", byStatus),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateStaffPerformanceReportAsync(
        Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format)
    {
        var attendance = await _db.StaffAttendances
            .Where(a => a.PropertyId == propertyId
                && a.CheckInTime >= startDate && a.CheckInTime <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var staffMembers = await _db.StaffMembers
            .Where(s => s.PropertyId == propertyId && s.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var property = await _db.Properties.FindAsync(propertyId);

        var staffSummary = staffMembers.Select(s =>
        {
            var records = attendance.Where(a => a.StaffId == s.Id).ToList();
            var totalHours = records
                .Where(a => a.CheckOutTime.HasValue)
                .Sum(a => (a.CheckOutTime!.Value - a.CheckInTime).TotalHours);
            return new ReportRow(
                $"{s.FirstName} {s.LastName}",
                $"{records.Count} days | {totalHours:F1}h total");
        }).ToList();

        var data = new ReportData
        {
            Title = "Staff Performance Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Summary", new List<ReportRow>
                {
                    new("Active Staff", staffMembers.Count.ToString()),
                    new("Total Attendance Records", attendance.Count.ToString()),
                }),
                new("Staff Breakdown", staffSummary),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateUpsellPerformanceReportAsync(
        Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format)
    {
        var transactions = await _db.Set<UpsellTransaction>()
            .Include(t => t.Offer)
            .Where(t => t.Offer.PropertyId == propertyId
                && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var offers = await _db.Set<UpsellOffer>()
            .Where(o => o.PropertyId == propertyId)
            .AsNoTracking()
            .ToListAsync();

        var property = await _db.Properties.FindAsync(propertyId);
        var totalRevenue = transactions.Sum(t => t.UnitPrice * t.Quantity);

        var offerBreakdown = offers.Select(o =>
        {
            var tx = transactions.Where(t => t.OfferId == o.Id).ToList();
            return new ReportRow(
                $"{o.Title} ({o.OfferType})",
                $"{tx.Count} purchases | {tx.Sum(t => t.UnitPrice * t.Quantity):C}");
        }).Where(r => !r.Value.StartsWith("0")).ToList();

        var data = new ReportData
        {
            Title = "Upsell Performance Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("KPIs", new List<ReportRow>
                {
                    new("Total Upsell Revenue", $"{totalRevenue:C}"),
                    new("Total Transactions", transactions.Count.ToString()),
                    new("Average Order Value", $"{(transactions.Count > 0 ? totalRevenue / transactions.Count : 0):C}"),
                    new("Active Offers", offers.Count(o => o.IsActive).ToString()),
                }),
                new("By Offer", offerBreakdown),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateGroupConsolidatedReportAsync(
        Guid groupId, DateTime startDate, DateTime endDate, ReportFormat format)
    {
        var group = await _db.Set<PropertyGroup>()
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new InvalidOperationException($"Property group {groupId} not found.");

        var propertyIds = group.Memberships.Select(m => m.PropertyId).ToList();
        var properties = await _db.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .AsNoTracking()
            .ToListAsync();

        var payments = await _db.Payments
            .IgnoreQueryFilters()
            .Where(p => propertyIds.Contains(p.PropertyId)
                && !p.IsDeleted
                && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var bookings = await _db.Bookings
            .IgnoreQueryFilters()
            .Where(b => propertyIds.Contains(b.PropertyId)
                && !b.IsDeleted
                && b.CheckInDate >= startDate && b.CheckInDate <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var propertyBreakdown = properties.Select(p =>
        {
            var revenue = payments.Where(pay => pay.PropertyId == p.Id).Sum(pay => pay.Amount);
            var propBookings = bookings.Where(b => b.PropertyId == p.Id).Count();
            return new ReportRow(p.Name, $"{revenue:C} | {propBookings} bookings");
        }).ToList();

        var data = new ReportData
        {
            Title = "Group Consolidated Report",
            PropertyName = group.Name,
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Group Summary", new List<ReportRow>
                {
                    new("Properties in Group", properties.Count.ToString()),
                    new("Total Revenue", $"{payments.Sum(p => p.Amount):C}"),
                    new("Total Bookings", bookings.Count.ToString()),
                }),
                new("By Property", propertyBreakdown),
            }
        };

        return FormatReport(data, format);
    }

    public async Task<byte[]> GenerateAiConciergeReportAsync(
        Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format)
    {
        var interactions = await _db.Set<AiInteraction>()
            .Where(i => i.PropertyId == propertyId
                && i.CreatedAt >= startDate && i.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var property = await _db.Properties.FindAsync(propertyId);
        var total = interactions.Count;

        var byIntent = interactions
            .GroupBy(i => i.IntentCategory ?? "Unknown")
            .Select(g => new ReportRow(g.Key, $"{g.Count()} ({g.Average(i => i.ConfidenceScore):P0} avg confidence)"))
            .ToList();

        var data = new ReportData
        {
            Title = "AI Concierge Report",
            PropertyName = property?.Name ?? "Unknown",
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Sections = new List<ReportSection>
            {
                new("Summary", new List<ReportRow>
                {
                    new("Total Interactions", total.ToString()),
                    new("Auto-Sent", interactions.Count(i => i.Outcome == AiInteractionOutcome.AutoSent).ToString()),
                    new("Staff-Approved", interactions.Count(i => i.Outcome == AiInteractionOutcome.Approved).ToString()),
                    new("Staff-Edited", interactions.Count(i => i.Outcome == AiInteractionOutcome.Edited).ToString()),
                    new("Average Confidence", $"{(total > 0 ? interactions.Average(i => i.ConfidenceScore) : 0):P0}"),
                }),
                new("By Intent Category", byIntent),
            }
        };

        return FormatReport(data, format);
    }

    // ─── Format Engine ───────────────────────────────────────────────

    private static byte[] FormatReport(ReportData data, ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Csv => FormatCsv(data),
            ReportFormat.Excel => FormatCsv(data), // CSV with .xlsx-compatible formatting
            ReportFormat.Pdf => FormatPdf(data),
            _ => FormatCsv(data),
        };
    }

    private static byte[] FormatCsv(ReportData data)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, System.Text.Encoding.UTF8);

        writer.WriteLine($"{data.Title}");
        writer.WriteLine($"Property:,{data.PropertyName}");
        writer.WriteLine($"Period:,{data.Period}");
        writer.WriteLine($"Generated:,{data.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
        writer.WriteLine();

        foreach (var section in data.Sections)
        {
            writer.WriteLine($"--- {section.Title} ---");
            foreach (var row in section.Rows)
            {
                writer.WriteLine($"{row.Label},{row.Value}");
            }
            writer.WriteLine();
        }

        writer.Flush();
        return ms.ToArray();
    }

    private static byte[] FormatPdf(ReportData data)
    {
        // Clean, structured HTML → PDF-ready output
        // In production, use QuestPDF or wkhtmltopdf to convert this to actual PDF bytes.
        // For now, returns a well-structured HTML document that renders as a clean report.
        var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>{data.Title}</title>
<style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #1B3A2D; }}
    h1 {{ color: #1B3A2D; border-bottom: 3px solid #D4A843; padding-bottom: 10px; }}
    h2 {{ color: #2D5A3F; margin-top: 30px; }}
    .meta {{ color: #666; font-size: 0.9em; margin-bottom: 20px; }}
    table {{ width: 100%; border-collapse: collapse; margin: 10px 0 20px 0; }}
    th, td {{ border: 1px solid #ddd; padding: 8px 12px; text-align: left; }}
    th {{ background-color: #1B3A2D; color: #F5F0E1; }}
    tr:nth-child(even) {{ background-color: #f9f9f9; }}
    .footer {{ margin-top: 40px; font-size: 0.8em; color: #999; border-top: 1px solid #ddd; padding-top: 10px; }}
</style>
</head>
<body>
<h1>{data.Title}</h1>
<div class=""meta"">
    <strong>Property:</strong> {data.PropertyName} |
    <strong>Period:</strong> {data.Period} |
    <strong>Generated:</strong> {data.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC
</div>";

        foreach (var section in data.Sections)
        {
            html += $@"
<h2>{section.Title}</h2>
<table>
<tr><th>Item</th><th>Value</th></tr>";
            foreach (var row in section.Rows)
            {
                html += $"<tr><td>{row.Label}</td><td>{row.Value}</td></tr>";
            }
            html += "</table>";
        }

        html += @"
<div class=""footer"">
    SAFARIstack PMS — Enterprise Report Engine v2.0<br>
    This report was automatically generated. Data is subject to system accuracy.
</div>
</body></html>";

        return System.Text.Encoding.UTF8.GetBytes(html);
    }
}

// ─── Internal Report Data Models ─────────────────────────────────────

internal record ReportData
{
    public string Title { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public string Period { get; init; } = string.Empty;
    public List<ReportSection> Sections { get; init; } = new();
}

internal record ReportSection(string Title, List<ReportRow> Rows);
internal record ReportRow(string Label, string Value);
