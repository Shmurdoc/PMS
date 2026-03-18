using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Modules.Staff.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Chart-ready JSON endpoints — serve time-series, breakdowns, and comparison data
/// for frontend graph/chart libraries (LiveCharts2, ApexCharts, Chart.js).
/// 
/// Graph tiers:
///   Simple  → single series (line, pie, bar)
///   Advanced → multi-series with comparisons
///   Ultra   → heatmaps, confidence bands, composite dashboards
/// </summary>
public static class ChartEndpoints
{
    public static void MapChartEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/charts")
            .WithTags("Charts & Analytics")
            .RequireAuthorization();

        // ═══════════════════════════════════════════════════════════════
        //  3.1  OCCUPANCY TREND — Simple Graph (line/area)
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/occupancy-trend/{propertyId:guid}", async (
            Guid propertyId,
            DateTime? from,
            DateTime? to,
            string? granularity, // day | week | month
            ApplicationDbContext db) =>
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-30);
            var endDate = to ?? DateTime.UtcNow;
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);
            if (totalRooms == 0) return Results.Ok(new { labels = Array.Empty<string>(), series = Array.Empty<object>() });

            var labels = new List<string>();
            var occupancyData = new List<decimal>();
            var availableData = new List<int>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                var occupied = await db.Bookings.CountAsync(b =>
                    b.PropertyId == propertyId &&
                    b.CheckInDate <= dateUtc &&
                    b.CheckOutDate > dateUtc &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));

                var pct = Math.Round((decimal)occupied / totalRooms * 100, 1);
                labels.Add(date.ToString("yyyy-MM-dd"));
                occupancyData.Add(pct);
                availableData.Add(totalRooms - occupied);
            }

            // Aggregate by granularity
            if (granularity?.ToLower() == "week")
                return Results.Ok(AggregateByWeek(labels, occupancyData, availableData));
            if (granularity?.ToLower() == "month")
                return Results.Ok(AggregateByMonth(labels, occupancyData, availableData));

            var avgOcc = occupancyData.Count > 0 ? Math.Round(occupancyData.Average(), 1) : 0;
            var peakIdx = occupancyData.Count > 0 ? occupancyData.IndexOf(occupancyData.Max()) : 0;

            return Results.Ok(new
            {
                labels,
                series = new object[]
                {
                    new { name = "Occupancy %", data = occupancyData },
                    new { name = "Available Rooms", data = availableData }
                },
                summary = new
                {
                    avgOccupancy = avgOcc,
                    peakDate = labels.Count > peakIdx ? labels[peakIdx] : "",
                    peakOccupancy = occupancyData.Count > 0 ? occupancyData.Max() : 0
                }
            });
        })
        .WithName("ChartOccupancyTrend").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.2  REVENUE BREAKDOWN — Advanced Graph (pie/donut)
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/revenue-breakdown/{propertyId:guid}", async (
            Guid propertyId,
            DateTime? from,
            DateTime? to,
            ApplicationDbContext db) =>
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-30);
            var endDate = to ?? DateTime.UtcNow;

            // Total revenue for current period
            var payments = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId &&
                            p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Revenue by folio line item category (Room, F&B, Spa, etc.)
            var lineItems = await db.Set<FolioLineItem>()
                .Where(li => li.Folio!.PropertyId == propertyId &&
                             li.CreatedAt >= startDate && li.CreatedAt <= endDate)
                .GroupBy(li => li.Description) // Group by description as proxy for category
                .Select(g => new { Category = g.Key ?? "Other", Total = g.Sum(li => li.UnitPrice * li.Quantity) })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToListAsync();

            var categories = lineItems.Select(x => x.Category).ToList();
            var values = lineItems.Select(x => x.Total).ToList();

            // Comparison with previous period
            var periodLength = (endDate - startDate).TotalDays;
            var prevStart = startDate.AddDays(-periodLength);
            var prevEnd = startDate;
            var prevRevenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId &&
                            p.PaymentDate >= prevStart && p.PaymentDate < prevEnd)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var changePct = prevRevenue > 0 ? Math.Round((payments - prevRevenue) / prevRevenue * 100, 1) : 0;

            return Results.Ok(new
            {
                categories,
                values,
                total = payments,
                currency = "ZAR",
                comparison = new { previousPeriod = prevRevenue, changePercent = changePct }
            });
        })
        .WithName("ChartRevenueBreakdown").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.3  REVENUE TREND — Time Series with ADR & RevPAR
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/revenue-trend/{propertyId:guid}", async (
            Guid propertyId,
            DateTime? from,
            DateTime? to,
            ApplicationDbContext db) =>
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-30);
            var endDate = to ?? DateTime.UtcNow;
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);

            var dailyRevenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId &&
                            p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(p => p.Amount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var labels = new List<string>();
            var revenueData = new List<decimal>();
            var adrData = new List<decimal>();
            var revparData = new List<decimal>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                var dayRev = dailyRevenue.FirstOrDefault(d => d.Date == date)?.Total ?? 0;
                var roomsSold = await db.Bookings.CountAsync(b =>
                    b.PropertyId == propertyId &&
                    b.CheckInDate <= dateUtc && b.CheckOutDate > dateUtc &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));

                var adr = roomsSold > 0 ? Math.Round(dayRev / roomsSold, 2) : 0;
                var revpar = totalRooms > 0 ? Math.Round(dayRev / totalRooms, 2) : 0;

                labels.Add(date.ToString("yyyy-MM-dd"));
                revenueData.Add(dayRev);
                adrData.Add(adr);
                revparData.Add(revpar);
            }

            return Results.Ok(new
            {
                labels,
                series = new object[]
                {
                    new { name = "Revenue", data = revenueData },
                    new { name = "ADR", data = adrData },
                    new { name = "RevPAR", data = revparData }
                }
            });
        })
        .WithName("ChartRevenueTrend").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.4  BOOKING SOURCE DISTRIBUTION — Simple Graph (pie)
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/booking-sources/{propertyId:guid}", async (
            Guid propertyId,
            DateTime? from,
            DateTime? to,
            ApplicationDbContext db) =>
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-90);
            var endDate = to ?? DateTime.UtcNow;

            var sources = await db.Bookings
                .Where(b => b.PropertyId == propertyId &&
                            b.CreatedAt >= startDate && b.CreatedAt <= endDate)
                .GroupBy(b => b.Source)
                .Select(g => new { Source = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var total = sources.Sum(s => s.Count);
            return Results.Ok(new
            {
                categories = sources.Select(s => s.Source),
                values = sources.Select(s => total > 0 ? Math.Round((decimal)s.Count / total * 100, 1) : 0),
                counts = sources.Select(s => s.Count),
                unit = "percent"
            });
        })
        .WithName("ChartBookingSources").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.5  GUEST DEMOGRAPHICS — Simple Graph (bar/pie)
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/guest-demographics/{propertyId:guid}", async (
            Guid propertyId,
            ApplicationDbContext db) =>
        {
            var byNationality = await db.Guests
                .Where(g => g.PropertyId == propertyId && g.Nationality != null)
                .GroupBy(g => g.Nationality!)
                .Select(g => new { label = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(15)
                .ToListAsync();

            var byGuestType = await db.Guests
                .Where(g => g.PropertyId == propertyId)
                .GroupBy(g => g.GuestType)
                .Select(g => new { label = g.Key.ToString(), count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            var byLoyaltyTier = await db.Set<GuestLoyalty>()
                .Where(gl => gl.Guest!.PropertyId == propertyId)
                .GroupBy(gl => gl.Tier)
                .Select(g => new { label = g.Key.ToString(), count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            return Results.Ok(new { byNationality, byGuestType, byLoyaltyTier });
        })
        .WithName("ChartGuestDemographics").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.6  SEASONAL DEMAND HEATMAP — Ultra Graph
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/seasonal-heatmap/{propertyId:guid}", async (
            Guid propertyId,
            int? year,
            ApplicationDbContext db) =>
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);
            if (totalRooms == 0) return Results.Ok(new { months = Array.Empty<string>(), data = Array.Empty<object>() });

            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var heatmapData = new List<object>();

            for (int m = 1; m <= 12; m++)
            {
                var daysInMonth = DateTime.DaysInMonth(targetYear, m);
                var days = new List<object>();

                for (int d = 1; d <= daysInMonth; d++)
                {
                    var date = new DateTime(targetYear, m, d, 0, 0, 0, DateTimeKind.Utc);
                    var occupied = await db.Bookings.CountAsync(b =>
                        b.PropertyId == propertyId &&
                        b.CheckInDate <= date && b.CheckOutDate > date &&
                        (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));

                    days.Add(new { day = d, occupancy = Math.Round((decimal)occupied / totalRooms * 100, 0) });
                }

                heatmapData.Add(new { month = months[m - 1], days });
            }

            return Results.Ok(new
            {
                months,
                data = heatmapData,
                colorScale = new { min = 0, mid = 50, max = 100 }
            });
        })
        .WithName("ChartSeasonalHeatmap").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.8  DEMAND FORECAST — Ultra Graph (confidence bands)
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/demand-forecast/{propertyId:guid}", async (
            Guid propertyId,
            int? daysAhead,
            ApplicationDbContext db) =>
        {
            var days = Math.Min(daysAhead ?? 30, 90);
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);
            if (totalRooms == 0) return Results.Ok(new { labels = Array.Empty<string>(), series = Array.Empty<object>() });

            // Use historical same-period data from last year as baseline
            var labels = new List<string>();
            var predicted = new List<decimal>();
            var upperBound = new List<decimal>();
            var lowerBound = new List<decimal>();

            for (int d = 1; d <= days; d++)
            {
                var futureDate = DateTime.UtcNow.Date.AddDays(d);
                var historicalDate = futureDate.AddYears(-1);

                // Historical occupancy for same day last year (±3 days window for smoothing)
                var windowStart = DateTime.SpecifyKind(historicalDate.AddDays(-3), DateTimeKind.Utc);
                var windowEnd = DateTime.SpecifyKind(historicalDate.AddDays(3), DateTimeKind.Utc);

                var historicalOccupied = await db.Bookings
                    .Where(b => b.PropertyId == propertyId &&
                                b.CheckInDate <= windowEnd && b.CheckOutDate > windowStart &&
                                b.Status != BookingStatus.Cancelled)
                    .CountAsync();

                // Already confirmed bookings for the future date
                var futureUtc = DateTime.SpecifyKind(futureDate, DateTimeKind.Utc);
                var confirmedFuture = await db.Bookings.CountAsync(b =>
                    b.PropertyId == propertyId &&
                    b.CheckInDate <= futureUtc && b.CheckOutDate > futureUtc &&
                    b.Status == BookingStatus.Confirmed);

                // Blend historical pattern with current bookings
                var historicalRate = totalRooms > 0 ? (decimal)historicalOccupied / totalRooms * 100 / 7 : 50; // averaged over 7-day window
                var currentRate = totalRooms > 0 ? (decimal)confirmedFuture / totalRooms * 100 : 0;
                var blended = Math.Round(Math.Max(currentRate, historicalRate * 0.6m + currentRate * 0.4m), 1);
                blended = Math.Clamp(blended, 0, 100);

                labels.Add(futureDate.ToString("yyyy-MM-dd"));
                predicted.Add(blended);
                upperBound.Add(Math.Min(100, Math.Round(blended * 1.15m, 1)));
                lowerBound.Add(Math.Max(0, Math.Round(blended * 0.85m, 1)));
            }

            return Results.Ok(new
            {
                labels,
                series = new object[]
                {
                    new { name = "Predicted Occupancy %", data = predicted },
                    new { name = "Upper Bound", data = upperBound },
                    new { name = "Lower Bound", data = lowerBound }
                },
                confidence = 0.85,
                influencingFactors = new[] { "Historical Pattern", "Current Bookings", "Seasonality" }
            });
        })
        .WithName("ChartDemandForecast").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.9  HOUSEKEEPING PERFORMANCE — Simple Graph
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/housekeeping-performance/{propertyId:guid}", async (
            Guid propertyId,
            DateTime? from,
            DateTime? to,
            ApplicationDbContext db) =>
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-7);
            var endDate = to ?? DateTime.UtcNow;

            var tasks = await db.HousekeepingTasks
                .Where(t => t.PropertyId == propertyId && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .AsNoTracking()
                .ToListAsync();

            var completed = tasks.Where(t => t.Status == HousekeepingTaskStatus.Completed || t.Status == HousekeepingTaskStatus.Inspected).ToList();
            var avgTime = completed
                .Where(t => t.StartedAt.HasValue && t.CompletedAt.HasValue)
                .Select(t => (t.CompletedAt!.Value - t.StartedAt!.Value).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var tasksByStatus = tasks
                .GroupBy(t => t.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Staff productivity
            var staffProductivity = tasks
                .Where(t => t.AssignedToStaffId.HasValue && t.CompletedAt.HasValue)
                .GroupBy(t => t.AssignedToStaffId!.Value)
                .Select(g => new
                {
                    staffId = g.Key,
                    tasksCompleted = g.Count(),
                    avgTimeMinutes = Math.Round(g
                        .Where(t => t.StartedAt.HasValue)
                        .Select(t => (t.CompletedAt!.Value - t.StartedAt!.Value).TotalMinutes)
                        .DefaultIfEmpty(0)
                        .Average(), 0)
                })
                .OrderByDescending(x => x.tasksCompleted)
                .Take(10)
                .ToList();

            // Daily trend
            var dailyTrend = tasks
                .Where(t => t.CompletedAt.HasValue)
                .GroupBy(t => t.CompletedAt!.Value.DayOfWeek)
                .OrderBy(g => g.Key)
                .Select(g => new { day = g.Key.ToString(), count = g.Count() })
                .ToList();

            return Results.Ok(new
            {
                avgCleaningTimeMinutes = Math.Round(avgTime, 0),
                tasksByStatus,
                staffProductivity,
                dailyTrend = new
                {
                    labels = dailyTrend.Select(d => d.day),
                    series = new[] { new { name = "Tasks Completed", data = dailyTrend.Select(d => d.count) } }
                }
            });
        })
        .WithName("ChartHousekeepingPerformance").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.10  FINANCIAL SUMMARY — Advanced Graph (composite dashboard)
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/financial-summary/{propertyId:guid}", async (
            Guid propertyId,
            DateTime? from,
            DateTime? to,
            ApplicationDbContext db) =>
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-30);
            var endDate = to ?? DateTime.UtcNow;

            var totalRevenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId &&
                            p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);
            var periodDays = Math.Max(1, (endDate - startDate).Days);

            // Calculate room nights sold
            var roomNightsSold = 0;
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                roomNightsSold += await db.Bookings.CountAsync(b =>
                    b.PropertyId == propertyId &&
                    b.CheckInDate <= dateUtc && b.CheckOutDate > dateUtc &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
            }

            var adr = roomNightsSold > 0 ? Math.Round(totalRevenue / roomNightsSold, 2) : 0;
            var revpar = totalRooms > 0 ? Math.Round(totalRevenue / (totalRooms * periodDays), 2) : 0;

            // Invoices outstanding
            var outstandingInvoices = await db.Invoices
                .Where(i => i.PropertyId == propertyId && i.Status != InvoiceStatus.Paid)
                .SumAsync(i => (decimal?)(i.TotalAmount - i.PaidAmount)) ?? 0;

            return Results.Ok(new
            {
                kpis = new
                {
                    totalRevenue,
                    adr,
                    revpar,
                    roomNightsSold,
                    totalRoomNights = totalRooms * periodDays,
                    occupancyRate = totalRooms > 0 ? Math.Round((decimal)roomNightsSold / (totalRooms * periodDays) * 100, 1) : 0,
                    outstandingInvoices
                },
                period = new { from = startDate, to = endDate, days = periodDays }
            });
        })
        .WithName("ChartFinancialSummary").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.11  STAFF OVERVIEW CHART
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/staff-overview/{propertyId:guid}", async (
            Guid propertyId,
            ApplicationDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

            var totalStaff = await db.StaffMembers.CountAsync(s => s.PropertyId == propertyId && s.IsActive);

            var onDutyToday = await db.StaffAttendances
                .CountAsync(a => a.PropertyId == propertyId &&
                                 a.CheckInTime.Date == todayUtc.Date &&
                                 a.CheckOutTime == null);

            var byDepartment = await db.StaffMembers
                .Where(s => s.PropertyId == propertyId && s.IsActive)
                .GroupBy(s => s.Role)
                .Select(g => new { dept = g.Key.ToString(), total = g.Count() })
                .ToListAsync();

            return Results.Ok(new
            {
                staffOnDuty = onDutyToday,
                totalStaff,
                attendanceRate = totalStaff > 0 ? Math.Round((decimal)onDutyToday / totalStaff * 100, 1) : 0,
                byDepartment
            });
        })
        .WithName("ChartStaffOverview").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  3.12  GUEST SATISFACTION — Review Trends
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/guest-satisfaction/{propertyId:guid}", (Guid propertyId) =>
        {
            // Stub — requires GuestFeedback entity (see §4.16 in Z new todo1.md)
            return Results.Ok(new
            {
                overallScore = 0.0,
                totalReviews = 0,
                byCategory = new { Cleanliness = 0.0, Service = 0.0, Location = 0.0, Value = 0.0, Amenities = 0.0 },
                trend = new { labels = Array.Empty<string>(), series = Array.Empty<object>() },
                sentimentBreakdown = new { Positive = 0, Neutral = 0, Negative = 0 },
                message = "GuestFeedback entity required — see Z new todo1.md §4.16"
            });
        })
        .WithName("ChartGuestSatisfaction").WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  PORTFOLIO SUMMARY (Multi-Property) — Advanced Graph
        // ═══════════════════════════════════════════════════════════════
        group.MapGet("/portfolio-summary", async (ApplicationDbContext db) =>
        {
            var properties = await db.Properties.AsNoTracking().ToListAsync();
            var today = DateTime.UtcNow.Date;
            var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
            var monthStart = DateTime.SpecifyKind(new DateTime(today.Year, today.Month, 1), DateTimeKind.Utc);

            var summaries = new List<object>();
            foreach (var prop in properties)
            {
                var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == prop.Id && r.IsActive);
                var occupied = await db.Bookings.CountAsync(b =>
                    b.PropertyId == prop.Id &&
                    b.CheckInDate <= todayUtc && b.CheckOutDate > todayUtc &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
                var monthRevenue = await db.Payments
                    .Where(p => p.Folio!.PropertyId == prop.Id && p.PaymentDate >= monthStart)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                summaries.Add(new
                {
                    propertyId = prop.Id,
                    propertyName = prop.Name,
                    totalRooms,
                    occupiedRooms = occupied,
                    occupancyRate = totalRooms > 0 ? Math.Round((decimal)occupied / totalRooms * 100, 1) : 0,
                    monthRevenue
                });
            }

            return Results.Ok(new { properties = summaries, generatedAt = DateTime.UtcNow });
        })
        .WithName("ChartPortfolioSummary").WithOpenApi()
        .RequireAuthorization("AdminOnly");
    }

    // ─── Helper methods for aggregation ──────────────────────────────

    private static object AggregateByWeek(List<string> labels, List<decimal> occupancy, List<int> available)
    {
        var weekLabels = new List<string>();
        var weekOcc = new List<decimal>();
        var weekAvail = new List<int>();

        for (int i = 0; i < labels.Count; i += 7)
        {
            var chunk = Math.Min(7, labels.Count - i);
            weekLabels.Add($"{labels[i]} — {labels[Math.Min(i + 6, labels.Count - 1)]}");
            weekOcc.Add(Math.Round(occupancy.Skip(i).Take(chunk).Average(), 1));
            weekAvail.Add((int)available.Skip(i).Take(chunk).Average());
        }

        return new
        {
            labels = weekLabels,
            series = new object[]
            {
                new { name = "Occupancy %", data = weekOcc },
                new { name = "Available Rooms", data = weekAvail }
            }
        };
    }

    private static object AggregateByMonth(List<string> labels, List<decimal> occupancy, List<int> available)
    {
        var grouped = labels.Select((l, i) => new { Label = l, Occ = occupancy[i], Avail = available[i] })
            .GroupBy(x => x.Label[..7]) // "yyyy-MM"
            .Select(g => new
            {
                Label = g.Key,
                Occ = Math.Round(g.Average(x => x.Occ), 1),
                Avail = (int)g.Average(x => x.Avail)
            })
            .ToList();

        return new
        {
            labels = grouped.Select(g => g.Label),
            series = new object[]
            {
                new { name = "Occupancy %", data = grouped.Select(g => g.Occ) },
                new { name = "Available Rooms", data = grouped.Select(g => g.Avail) }
            }
        };
    }
}
