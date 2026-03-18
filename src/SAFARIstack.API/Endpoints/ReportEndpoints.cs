using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization("ManagerOrAbove");

        // Daily Operations
        group.MapGet("/daily-operations/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime date,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateDailyOperationsReportAsync(propertyId, date, format);
            return Results.File(bytes, GetContentType(format), $"daily-operations-{date:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateDailyOperationsReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Monthly Financial
        group.MapGet("/monthly-financial/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateMonthlyFinancialReportAsync(propertyId, year, month, format);
            return Results.File(bytes, GetContentType(format), $"financial-{year}-{month:D2}.{GetExtension(format)}");
        })
        .WithName("GenerateMonthlyFinancialReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Occupancy
        group.MapGet("/occupancy/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateOccupancyReportAsync(propertyId, startDate, endDate, format);
            return Results.File(bytes, GetContentType(format), $"occupancy-{startDate:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateOccupancyReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Revenue
        group.MapGet("/revenue/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateRevenueReportAsync(propertyId, startDate, endDate, format);
            return Results.File(bytes, GetContentType(format), $"revenue-{startDate:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateRevenueReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Guest Analytics
        group.MapGet("/guest-analytics/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateGuestAnalyticsReportAsync(propertyId, startDate, endDate, format);
            return Results.File(bytes, GetContentType(format), $"guest-analytics-{startDate:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateGuestAnalyticsReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Housekeeping
        group.MapGet("/housekeeping/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime date,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateHousekeepingReportAsync(propertyId, date, format);
            return Results.File(bytes, GetContentType(format), $"housekeeping-{date:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateHousekeepingReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Staff Performance
        group.MapGet("/staff-performance/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateStaffPerformanceReportAsync(propertyId, startDate, endDate, format);
            return Results.File(bytes, GetContentType(format), $"staff-performance-{startDate:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateStaffPerformanceReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Upsell Performance
        group.MapGet("/upsell-performance/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateUpsellPerformanceReportAsync(propertyId, startDate, endDate, format);
            return Results.File(bytes, GetContentType(format), $"upsell-performance-{startDate:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateUpsellPerformanceReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // Group Consolidated
        group.MapGet("/group-consolidated/{groupId:guid}", async (
            Guid groupId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateGroupConsolidatedReportAsync(groupId, startDate, endDate, format);
            return Results.File(bytes, GetContentType(format), $"group-consolidated-{startDate:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateGroupConsolidatedReport")
        .WithOpenApi()
        .RequireAuthorization("AdminOnly")
        .Produces(StatusCodes.Status200OK);

        // AI Concierge Report
        group.MapGet("/ai-concierge/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] ReportFormat format,
            IReportService svc) =>
        {
            var bytes = await svc.GenerateAiConciergeReportAsync(propertyId, startDate, endDate, format);
            return Results.File(bytes, GetContentType(format), $"ai-concierge-{startDate:yyyy-MM-dd}.{GetExtension(format)}");
        })
        .WithName("GenerateAiConciergeReport")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);
    }

    private static string GetContentType(ReportFormat format) => format switch
    {
        ReportFormat.Pdf => "text/html", // HTML until QuestPDF integrated → then "application/pdf"
        ReportFormat.Excel => "text/csv",
        ReportFormat.Csv => "text/csv",
        _ => "text/csv"
    };

    private static string GetExtension(ReportFormat format) => format switch
    {
        ReportFormat.Pdf => "html", // HTML until QuestPDF integrated → then "pdf"
        ReportFormat.Excel => "csv",
        ReportFormat.Csv => "csv",
        _ => "csv"
    };
}
