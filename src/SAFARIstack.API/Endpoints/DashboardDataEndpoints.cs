using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Dashboard data endpoints for KPIs, charts, and time-series metrics.
/// Provides real-time operational insights optimized for frontend visualization.
/// </summary>
public static class DashboardDataEndpoints
{
    public static void MapDashboardDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithOpenApi()
            .RequireAuthorization();

        // KPI Metrics
        group.MapGet("/{propertyId}/kpi/summary", GetKpiSummary)
            .WithName("GetKpiSummary")
            .Produces<KpiSummaryDto>();

        group.MapGet("/{propertyId}/kpi/daily", GetDailyKpi)
            .WithName("GetDailyKpi")
            .Produces<DailyKpiDto>();

        group.MapGet("/{propertyId}/kpi/monthly", GetMonthlyKpi)
            .WithName("GetMonthlyKpi")
            .Produces<MonthlyKpiDto>();

        // Chart Data
        group.MapGet("/{propertyId}/chart/revenue-timeline", GetRevenueTimeline)
            .WithName("GetRevenueTimeline")
            .Produces<TimelineChartDto>();

        group.MapGet("/{propertyId}/chart/occupancy-timeline", GetOccupancyTimeline)
            .WithName("GetOccupancyTimeline")
            .Produces<TimelineChartDto>();

        group.MapGet("/{propertyId}/chart/adr-timeline", GetAdrTimeline)
            .WithName("GetAdrTimeline")
            .Produces<TimelineChartDto>();

        group.MapGet("/{propertyId}/chart/room-status", GetRoomStatusChart)
            .WithName("GetRoomStatusChart")
            .Produces<PieChartDto>();

        // Time-Series
        group.MapGet("/{propertyId}/timeseries/revenue/days", GetRevenueTimeSeries)
            .WithName("GetRevenueTimeSeries")
            .Produces<TimeSeriesDto>();

        group.MapGet("/{propertyId}/timeseries/occupancy/days", GetOccupancyTimeSeries)
            .WithName("GetOccupancyTimeSeries")
            .Produces<TimeSeriesDto>();

        // Comparison
        group.MapGet("/{propertyId}/comparison/vs-last-month", GetComparisonLastMonth)
            .WithName("GetComparisonLastMonth")
            .Produces<ComparisonDto>();

        // Export
        group.MapGet("/{propertyId}/export/monthly", ExportMonthly)
            .WithName("ExportMonthly")
            .Produces<MonthlyExportDto>();
    }

    // ════════════════════════════════════════════════════════════════════════════════
    //  KPI ENDPOINTS
    // ════════════════════════════════════════════════════════════════════════════════

    private static async Task<Ok<KpiSummaryDto>> GetKpiSummary(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var monthBookings = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= monthStart && b.CheckInDate <= today)
            .ToListAsync(ct);

        var totalRevenue = monthBookings.Sum(b => b.TotalAmount);
        var roomCount = await db.Rooms
            .Where(r => r.PropertyId == propertyId)
            .CountAsync(ct);
        var occupiedRooms = await db.Rooms
            .Where(r => r.PropertyId == propertyId && r.Status == RoomStatus.Occupied)
            .CountAsync(ct);

        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var revPar = roomCount > 0 ? totalRevenue / daysInMonth / roomCount : 0m;
        var occupancyRate = roomCount > 0 ? (decimal)occupiedRooms / roomCount * 100 : 0m;
        var adr = monthBookings.Count > 0 ? totalRevenue / monthBookings.Count : 0m;

        return TypedResults.Ok(new KpiSummaryDto(
            PropertyId: propertyId,
            GeneratedAt: DateTime.UtcNow,
            TotalRevenue: totalRevenue,
            AverageDailyRate: adr,
            RevenuePerAvailableRoom: revPar,
            OccupancyRate: occupancyRate,
            TotalRooms: roomCount,
            OccupiedRooms: occupiedRooms,
            BookingCount: monthBookings.Count));
    }

    private static async Task<Ok<DailyKpiDto>> GetDailyKpi(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var todayBookings = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate == today)
            .ToListAsync(ct);

        var todayRevenue = todayBookings.Sum(b => b.TotalAmount);

        return TypedResults.Ok(new DailyKpiDto(
            PropertyId: propertyId,
            Date: today,
            GeneratedAt: DateTime.UtcNow,
            DailyRevenue: todayRevenue,
            BookingCount: todayBookings.Count));
    }

    private static async Task<Ok<MonthlyKpiDto>> GetMonthlyKpi(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var monthBookings = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= monthStart && b.CheckInDate <= today)
            .ToListAsync(ct);

        var totalRevenue = monthBookings.Sum(b => b.TotalAmount);

        return TypedResults.Ok(new MonthlyKpiDto(
            PropertyId: propertyId,
            Month: today.Month,
            Year: today.Year,
            GeneratedAt: DateTime.UtcNow,
            TotalRevenue: totalRevenue,
            BookingCount: monthBookings.Count,
            AverageDailyRevenue: monthBookings.Count > 0 ? totalRevenue / monthBookings.Count : 0m));
    }

    // ════════════════════════════════════════════════════════════════════════════════
    //  CHART ENDPOINTS
    // ════════════════════════════════════════════════════════════════════════════════

    private static async Task<Ok<TimelineChartDto>> GetRevenueTimeline(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-29);

        var revenueData = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= thirtyDaysAgo && b.CheckInDate <= today)
            .GroupBy(b => b.CheckInDate)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(b => b.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var labels = Enumerable.Range(0, 30)
            .Select(i => today.AddDays(-29 + i).ToString("MMM dd"))
            .ToList();

        var values = new List<decimal>();
        for (int i = 0; i < 30; i++)
        {
            var date = today.AddDays(-29 + i).Date;
            var revenue = revenueData.FirstOrDefault(x => x.Date == date)?.Revenue ?? 0m;
            values.Add(revenue);
        }

        return TypedResults.Ok(new TimelineChartDto(
            Title: "Revenue (Last 30 Days)",
            Unit: "ZAR",
            Labels: labels,
            Values: values));
    }

    private static async Task<Ok<TimelineChartDto>> GetOccupancyTimeline(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-29);

        var totalRooms = await db.Rooms
            .Where(r => r.PropertyId == propertyId)
            .CountAsync(ct);

        var occupancyData = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= thirtyDaysAgo && b.CheckInDate <= today)
            .GroupBy(b => b.CheckInDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var labels = Enumerable.Range(0, 30)
            .Select(i => today.AddDays(-29 + i).ToString("MMM dd"))
            .ToList();

        var values = new List<decimal>();
        for (int i = 0; i < 30; i++)
        {
            var date = today.AddDays(-29 + i).Date;
            var occupied = occupancyData.FirstOrDefault(x => x.Date == date)?.Count ?? 0;
            var rate = totalRooms > 0 ? (decimal)occupied / totalRooms * 100 : 0m;
            values.Add(Math.Round(rate, 2));
        }

        return TypedResults.Ok(new TimelineChartDto(
            Title: "Occupancy Rate (Last 30 Days)",
            Unit: "%",
            Labels: labels,
            Values: values));
    }

    private static async Task<Ok<TimelineChartDto>> GetAdrTimeline(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-29);

        var adrData = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= thirtyDaysAgo && b.CheckInDate <= today)
            .GroupBy(b => b.CheckInDate)
            .Select(g => new
            {
                Date = g.Key,
                ADR = g.Any() ? g.Sum(b => b.TotalAmount) / g.Count() : 0m
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var labels = Enumerable.Range(0, 30)
            .Select(i => today.AddDays(-29 + i).ToString("MMM dd"))
            .ToList();

        var values = new List<decimal>();
        for (int i = 0; i < 30; i++)
        {
            var date = today.AddDays(-29 + i).Date;
            var adr = adrData.FirstOrDefault(x => x.Date == date)?.ADR ?? 0m;
            values.Add(Math.Round(adr, 2));
        }

        return TypedResults.Ok(new TimelineChartDto(
            Title: "Average Daily Rate (Last 30 Days)",
            Unit: "ZAR",
            Labels: labels,
            Values: values));
    }

    private static async Task<Ok<PieChartDto>> GetRoomStatusChart(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var rooms = await db.Rooms
            .Where(r => r.PropertyId == propertyId)
            .ToListAsync(ct);

        var occupied = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var available = rooms.Count(r => r.Status == RoomStatus.Available);
        var cleaning = rooms.Count(r => r.Status == RoomStatus.Cleaning);

        return TypedResults.Ok(new PieChartDto(
            Title: "Room Status Distribution",
            Labels: new[] { "Occupied", "Available", "Cleaning" },
            Values: new decimal[] { occupied, available, cleaning }));
    }

    // ════════════════════════════════════════════════════════════════════════════════
    //  TIME-SERIES ENDPOINTS
    // ════════════════════════════════════════════════════════════════════════════════

    private static async Task<Ok<TimeSeriesDto>> GetRevenueTimeSeries(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-29);

        var series = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= thirtyDaysAgo && b.CheckInDate <= today)
            .GroupBy(b => b.CheckInDate)
            .Select(g => new TimeSeriesPoint(g.Key, g.Sum(b => b.TotalAmount)))
            .OrderBy(x => x.Timestamp)
            .ToListAsync(ct);

        return TypedResults.Ok(new TimeSeriesDto(
            Metric: "revenue",
            Unit: "ZAR",
            Data: series,
            GeneratedAt: DateTime.UtcNow));
    }

    private static async Task<Ok<TimeSeriesDto>> GetOccupancyTimeSeries(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-29);

        var totalRooms = await db.Rooms.Where(r => r.PropertyId == propertyId).CountAsync(ct);

        var occupancyData = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= thirtyDaysAgo && b.CheckInDate <= today)
            .GroupBy(b => b.CheckInDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var series = occupancyData
            .Select(x => new TimeSeriesPoint(
                Timestamp: x.Date,
                Value: totalRooms > 0 ? Math.Round((decimal)x.Count / totalRooms * 100, 2) : 0m))
            .ToList();

        return TypedResults.Ok(new TimeSeriesDto(
            Metric: "occupancy",
            Unit: "%",
            Data: series,
            GeneratedAt: DateTime.UtcNow));
    }

    // ════════════════════════════════════════════════════════════════════════════════
    //  COMPARISON ENDPOINTS
    // ════════════════════════════════════════════════════════════════════════════════

    private static async Task<Ok<ComparisonDto>> GetComparisonLastMonth(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = currentMonthStart.AddMonths(-1);
        var lastMonthEnd = currentMonthStart.AddDays(-1);

        var currentMonth = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= currentMonthStart && b.CheckInDate <= today)
            .ToListAsync(ct);

        var lastMonth = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= lastMonthStart && b.CheckInDate <= lastMonthEnd)
            .ToListAsync(ct);

        var currentRevenue = currentMonth.Sum(b => b.TotalAmount);
        var lastRevenue = lastMonth.Sum(b => b.TotalAmount);
        var trend = lastRevenue > 0 ? ((currentRevenue - lastRevenue) / lastRevenue * 100) : 0m;

        return TypedResults.Ok(new ComparisonDto(
            Period: $"{lastMonthStart:MMMM yyyy} vs {currentMonthStart:MMMM yyyy}",
            CurrentRevenue: currentRevenue,
            PreviousRevenue: lastRevenue,
            TrendPercentage: trend,
            GeneratedAt: DateTime.UtcNow));
    }

    // ════════════════════════════════════════════════════════════════════════════════
    //  EXPORT ENDPOINTS
    // ════════════════════════════════════════════════════════════════════════════════

    private static async Task<Ok<MonthlyExportDto>> ExportMonthly(
        Guid propertyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var monthBookings = await db.Bookings
            .Where(b => b.PropertyId == propertyId && b.CheckInDate >= monthStart && b.CheckInDate <= today)
            .ToListAsync(ct);

        var revenue = monthBookings.Sum(b => b.TotalAmount);

        return TypedResults.Ok(new MonthlyExportDto(
            PropertyId: propertyId,
            Month: today.Month,
            Year: today.Year,
            TotalRevenue: revenue,
            BookingCount: monthBookings.Count,
            ExportedAt: DateTime.UtcNow));
    }
}

// ════════════════════════════════════════════════════════════════════════════════
//  DTOs
// ════════════════════════════════════════════════════════════════════════════════

public record KpiSummaryDto(
    Guid PropertyId,
    DateTime GeneratedAt,
    decimal TotalRevenue,
    decimal AverageDailyRate,
    decimal RevenuePerAvailableRoom,
    decimal OccupancyRate,
    int TotalRooms,
    int OccupiedRooms,
    int BookingCount);

public record DailyKpiDto(
    Guid PropertyId,
    DateTime Date,
    DateTime GeneratedAt,
    decimal DailyRevenue,
    int BookingCount);

public record MonthlyKpiDto(
    Guid PropertyId,
    int Month,
    int Year,
    DateTime GeneratedAt,
    decimal TotalRevenue,
    int BookingCount,
    decimal AverageDailyRevenue);

public record TimelineChartDto(
    string Title,
    string Unit,
    List<string> Labels,
    List<decimal> Values);

public record PieChartDto(
    string Title,
    string[] Labels,
    decimal[] Values);

public record TimeSeriesDto(
    string Metric,
    string Unit,
    List<TimeSeriesPoint> Data,
    DateTime GeneratedAt);

public record TimeSeriesPoint(
    DateTime Timestamp,
    decimal Value);

public record ComparisonDto(
    string Period,
    decimal CurrentRevenue,
    decimal PreviousRevenue,
    decimal TrendPercentage,
    DateTime GeneratedAt);

public record MonthlyExportDto(
    Guid PropertyId,
    int Month,
    int Year,
    decimal TotalRevenue,
    int BookingCount,
    DateTime ExportedAt);
