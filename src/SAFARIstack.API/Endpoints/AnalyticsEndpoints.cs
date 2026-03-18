using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SAFARIstack.Modules.Analytics.Domain.Interfaces;
using SAFARIstack.Modules.Analytics.Domain.Events;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Analytics endpoints for accessing KPIs, forecasts, and reports
/// </summary>
public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics")
            .WithTags("Analytics")
            .RequireAuthorization("ManagerOrAbove")
            .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  FORECAST ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get occupancy forecast for next N days
        /// </summary>
        group.MapGet("/occupancy-forecast/{propertyId}", GetOccupancyForecast)
            .WithName("GetOccupancyForecast")
            .WithSummary("Get occupancy forecast")
            .Produces<OccupancyForecastDto>(StatusCodes.Status200OK);

        /// <summary>
        /// Get revenue forecast for date range
        /// </summary>
        group.MapGet("/revenue-forecast/{propertyId}", GetRevenueForecast)
            .WithName("GetRevenueForecast")
            .WithSummary("Get revenue forecast")
            .Produces<RevenueForecastDto>(StatusCodes.Status200OK);

        /// <summary>
        /// Get guest behavior analytics
        /// </summary>
        group.MapGet("/guest-behavior/{propertyId}", GetGuestBehaviorAnalytics)
            .WithName("GetGuestBehaviorAnalytics")
            .WithSummary("Get guest behavior insights")
            .Produces<GuestBehaviorDto>(StatusCodes.Status200OK);

        /// <summary>
        /// Generate custom report by definition
        /// </summary>
        group.MapPost("/reports/generate", GenerateReport)
            .WithName("GenerateReport")
            .WithSummary("Generate custom report")
            .Produces<ReportResultDto>(StatusCodes.Status202Accepted);

        /// <summary>
        /// Get real-time dashboard metrics
        /// </summary>
        group.MapGet("/dashboard/{propertyId}", GetDashboardMetrics)
            .WithName("GetDashboardMetrics")
            .WithSummary("Get dashboard KPIs")
            .Produces<DashboardMetricsDto>(StatusCodes.Status200OK);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ENDPOINT HANDLERS
    // ═══════════════════════════════════════════════════════════════════

    private static async Task<IResult> GetOccupancyForecast(
        Guid propertyId,
        [FromQuery] int daysAhead = 30,
        IAnalyticsService analytics = null!)
    {
        try
        {
            var forecast = await analytics.GetOccupancyForecast(propertyId, daysAhead);
            return Results.Ok(forecast);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetRevenueForecast(
        Guid propertyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        IAnalyticsService analytics = null!)
    {
        try
        {
            if (startDate >= endDate)
                return Results.BadRequest(new { error = "startDate must be before endDate" });

            var period = new DateRange(startDate, endDate);
            var forecast = await analytics.GetRevenueForecast(propertyId, period);
            return Results.Ok(forecast);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetGuestBehaviorAnalytics(
        Guid propertyId,
        IGuestBehaviorAnalytics guestAnalytics = null!)
    {
        try
        {
            var segments = await guestAnalytics.GetHighValueSegments(propertyId, 10);
            return Results.Ok(segments);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GenerateReport(
        ReportGenerationRequest request,
        IAnalyticsService analytics = null!)
    {
        try
        {
            if (request.ReportDefinitionId == Guid.Empty)
                return Results.BadRequest(new { error = "ReportDefinitionId is required" });

            var definition = new ReportDefinition
            {
                ReportId = request.ReportDefinitionId,
                PropertyId = request.PropertyId,
                ReportType = request.ReportType ?? "Operational"
            };

            var result = await analytics.GenerateReport(definition);
            return Results.Accepted($"/api/analytics/reports/{result.ReportInstanceId}", result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetDashboardMetrics(
        Guid propertyId,
        IAnalyticsService analytics = null!)
    {
        try
        {
            var forecast = await analytics.GetOccupancyForecast(propertyId, 7);
            return Results.Ok(new DashboardMetricsDto(
                PropertyId: propertyId,
                CurrentOccupancy: forecast.PredictedOccupancyRate,
                DailyRevenue: 0, // Would need revenue service
                CheckInsToday: forecast.PredictedBookedRooms,
                CheckOutsToday: 0,
                OpenWorkOrders: 0
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  DTOs
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Occupancy forecast data transfer object
/// </summary>
public record OccupancyForecastDto(
    Guid PropertyId,
    DateTime StartDate,
    DateTime EndDate,
    decimal AverageOccupancy,
    int TotalRooms,
    int ForecastedBookings);

/// <summary>
/// Revenue forecast data transfer object
/// </summary>
public record RevenueForecastDto(
    Guid PropertyId,
    DateTime StartDate,
    DateTime EndDate,
    decimal ForecastedRevenue,
    decimal ActualRevenue,
    string Currency = "USD");

/// <summary>
/// Guest behavior analytics data transfer object
/// </summary>
public record GuestBehaviorDto(
    Guid PropertyId,
    decimal AverageStayLength,
    decimal RepeatGuestRate,
    string TopSourceMarket,
    decimal OnTimeArrivalRate);

/// <summary>
/// Dashboard metrics data transfer object
/// </summary>
public record DashboardMetricsDto(
    Guid PropertyId,
    decimal CurrentOccupancy,
    decimal DailyRevenue,
    int CheckInsToday,
    int CheckOutsToday,
    int OpenWorkOrders);

/// <summary>
/// Report generation request
/// </summary>
public record ReportGenerationRequest(
    Guid ReportDefinitionId,
    Guid PropertyId,
    string? ReportType = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null);

/// <summary>
/// Report generation result
/// </summary>
public record ReportResultDto(
    Guid ReportInstanceId,
    Guid PropertyId,
    string ReportType,
    DateTime GeneratedAt,
    string Status = "Generated",
    string? DownloadUrl = null);
