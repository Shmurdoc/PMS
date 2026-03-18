using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SAFARIstack.Modules.Revenue.Domain.Interfaces;
using SAFARIstack.Modules.Revenue.Domain.Models;
using System;
using System.Threading.Tasks;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Revenue management endpoints for pricing, revenue optimization, and demand signals
/// </summary>
public static class RevenueEndpoints
{
    public static void MapRevenueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/revenue")
            .WithTags("Revenue Management")
            .RequireAuthorization("ManagerOrAbove")
            .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  PRICING ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get recommended price for date range and room type
        /// </summary>
        group.MapGet("/price-recommendation/{propertyId}", GetPriceRecommendation)
            .WithName("GetPriceRecommendation")
            .WithSummary("Get AI-powered price recommendation")
            .Produces<PriceRecommendationDto>(StatusCodes.Status200OK);

        /// <summary>
        /// Apply dynamic pricing algorithm
        /// </summary>
        group.MapPost("/apply-dynamic-pricing", AcceptPricingRecommendation)
            .WithName("AcceptPricingRecommendation")
            .WithSummary("Accept pricing recommendation and apply rates")
            .Produces<DynamicPricingResultDto>(StatusCodes.Status200OK);

        // ═══════════════════════════════════════════════════════════════
        //  DEMAND SIGNAL ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get current demand signals from market analysis
        /// </summary>
        group.MapGet("/demand-signals/{propertyId}", GetDemandSignals)
            .WithName("GetDemandSignals")
            .WithSummary("Get market demand signals")
            .Produces<DemandSignalsDto>(StatusCodes.Status200OK);

        /// <summary>
        /// Get competitor pricing analysis
        /// </summary>
        group.MapGet("/competitor-analysis/{propertyId}", GetCompetitorAnalysis)
            .WithName("GetCompetitorAnalysis")
            .WithSummary("Get competitive market analysis")
            .Produces<CompetitorAnalysisDto>(StatusCodes.Status200OK);

        // ═══════════════════════════════════════════════════════════════
        //  REVENUE OPTIMIZATION ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get revenue optimization suggestions
        /// </summary>
        group.MapGet("/optimization-suggestions/{propertyId}", GetOptimizationSuggestions)
            .WithName("GetOptimizationSuggestions")
            .WithSummary("Get revenue optimization recommendations")
            .Produces<OptimizationSuggestionsDto>(StatusCodes.Status200OK);

        /// <summary>
        /// Get revenue performance metrics
        /// </summary>
        group.MapGet("/performance/{propertyId}", GetRevenuePerformance)
            .WithName("GetRevenuePerformance")
            .WithSummary("Get revenue performance KPIs")
            .Produces<RevenuePerformanceDto>(StatusCodes.Status200OK);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ENDPOINT HANDLERS
    // ═══════════════════════════════════════════════════════════════════

    private static async Task<IResult> GetPriceRecommendation(
        Guid propertyId,
        [FromQuery] Guid roomTypeId,
        [FromQuery] DateTime date,
        IRevenueManagementSystem rms = null!)
    {
        try
        {
            var recommendation = await rms.GeneratePricingRecommendationAsync(propertyId, roomTypeId, date);
            return Results.Ok(recommendation);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AcceptPricingRecommendation(
        DynamicPricingRequest request,
        IRevenueManagementSystem rms = null!)
    {
        try
        {
            if (request.PropertyId == Guid.Empty)
                return Results.BadRequest(new { error = "PropertyId is required" });

            var success = await rms.AcceptPricingRecommendationAsync(request.PropertyId);
            return Results.Ok(new DynamicPricingResultDto(
                PropertyId: request.PropertyId,
                RoomTypesUpdated: success ? 1 : 0,
                AveragePriceChange: 0,
                EffectiveDate: DateTime.UtcNow.AddDays(1),
                Success: success
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetDemandSignals(
        Guid propertyId,
        [FromQuery] DateTime date,
        IRevenueManagementSystem rms = null!)
    {
        try
        {
            var signal = await rms.GetDemandSignalAsync(propertyId, date);
            var demandLevel = signal.ConversionRate > 0.7m ? "High" : signal.ConversionRate > 0.4m ? "Medium" : "Low";
            
            return Results.Ok(new DemandSignalsDto(
                PropertyId: propertyId,
                DemandLevel: signal.ConversionRate,
                DemandLevelCategory: demandLevel,
                AvailableRooms: 0, // Would need to query from property service
                BookedRooms: signal.BookingsConfirmed,
                BookingVelocity: signal.ConversionRate,
                AsOfDate: date
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetCompetitorAnalysis(
        Guid propertyId,
        [FromQuery] string roomType,
        [FromQuery] DateTime date,
        IRevenueManagementSystem rms = null!)
    {
        try
        {
            var insight = await rms.GetRateShoppingInsightAsync(propertyId, roomType, date);
            return Results.Ok(new CompetitorAnalysisDto(
                PropertyId: propertyId,
                CompetitorsTracked: 3, // Booking.com, Expedia, Airbnb
                AverageCompetitorPrice: insight.AverageCompetitorRate,
                YourAveragePrice: insight.OurRate,
                PriceCompetitiveness: insight.RateIndex,
                PricingStrategy: insight.Recommendation ?? "maintain"
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetOptimizationSuggestions(
        Guid propertyId,
        IRevenueManagementSystem rms = null!)
    {
        try
        {
            var alert = await rms.CheckForRevenueAlertAsync(propertyId);
            var suggestions = alert != null 
                ? new[] { alert.Message }
                : new[] { "No immediate opportunities or risks detected" };
            
            var estimatedImpact = alert?.RecommendedActions.TryGetValue("estimatedRevenue", out var revenue) == true
                ? Convert.ToDecimal(revenue)
                : 0m;
                
            return Results.Ok(new OptimizationSuggestionsDto(
                PropertyId: propertyId,
                Suggestions: suggestions,
                PotentialRevenueIncrease: estimatedImpact,
                PriorityArea: alert?.AlertType ?? "Monitoring",
                GeneratedAt: DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetRevenuePerformance(
        Guid propertyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        IRevenueManagementSystem rms = null!)
    {
        try
        {
            // Would normally aggregate data from multiple sources
            return Results.Ok(new RevenuePerformanceDto(
                PropertyId: propertyId,
                StartDate: startDate,
                EndDate: endDate,
                TotalRevenue: 0,
                TargetRevenue: 0,
                RevenuePacing: 1.0m,
                RevenuePerAvailableRoom: 0,
                AverageRoomRate: 0
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
/// Price recommendation data transfer object
/// </summary>
public record PriceRecommendationDto(
    Guid PropertyId,
    Guid RoomTypeId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    decimal RecommendedPrice,
    decimal CurrentPrice,
    decimal PriceChangePercentage,
    string Reason);

/// <summary>
/// Dynamic pricing result data transfer object
/// </summary>
public record DynamicPricingResultDto(
    Guid PropertyId,
    int RoomTypesUpdated,
    decimal AveragePriceChange,
    DateTime EffectiveDate,
    bool Success);

/// <summary>
/// Demand signals data transfer object
/// </summary>
public record DemandSignalsDto(
    Guid PropertyId,
    decimal DemandLevel,
    string DemandLevelCategory,
    int AvailableRooms,
    int BookedRooms,
    decimal BookingVelocity,
    DateTime AsOfDate);

/// <summary>
/// Competitor analysis data transfer object
/// </summary>
public record CompetitorAnalysisDto(
    Guid PropertyId,
    int CompetitorsTracked,
    decimal AverageCompetitorPrice,
    decimal YourAveragePrice,
    decimal PriceCompetitiveness,
    string PricingStrategy);

/// <summary>
/// Optimization suggestions data transfer object
/// </summary>
public record OptimizationSuggestionsDto(
    Guid PropertyId,
    string[] Suggestions,
    decimal PotentialRevenueIncrease,
    string PriorityArea,
    DateTime GeneratedAt);

/// <summary>
/// Revenue performance data transfer object
/// </summary>
public record RevenuePerformanceDto(
    Guid PropertyId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRevenue,
    decimal TargetRevenue,
    decimal RevenuePacing,
    decimal RevenuePerAvailableRoom,
    decimal AverageRoomRate);

/// <summary>
/// Dynamic pricing request
/// </summary>
public record DynamicPricingRequest(
    Guid PropertyId,
    string[] AppliedStrategies = null!,
    DateTime? EffectiveDate = null);
