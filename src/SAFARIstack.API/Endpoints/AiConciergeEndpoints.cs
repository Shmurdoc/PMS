using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class AiConciergeEndpoints
{
    public static void MapAiConciergeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ai-concierge")
            .WithTags("AI Concierge")
            .RequireAuthorization();

        // Handle guest inquiry
        group.MapPost("/inquiry", async (
            AiInquiryRequest request,
            IAiConciergeService svc) =>
        {
            var context = new AiContextDto(
                request.PropertyId, request.GuestId, request.BookingId,
                request.GuestName, request.CheckInDate, request.CheckOutDate,
                request.RoomType, request.LoyaltyTier, request.Source);

            var result = await svc.HandleInquiryAsync(request.Question, context);
            return Results.Ok(result);
        })
        .WithName("HandleAiInquiry")
        .WithOpenApi()
        .Produces<AiConciergeResponseDto>(StatusCodes.Status200OK);

        // AI analytics for property
        group.MapGet("/analytics/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            IAiConciergeService svc) =>
        {
            var result = await svc.GetAiAnalyticsAsync(propertyId, startDate, endDate);
            return Results.Ok(result);
        })
        .WithName("GetAiAnalytics")
        .WithOpenApi()
        .RequireAuthorization("ManagerOrAbove")
        .Produces<AiAnalyticsDto>(StatusCodes.Status200OK);

        // Get improvement suggestions
        group.MapGet("/improvements/{propertyId:guid}", async (
            Guid propertyId,
            IAiConciergeService svc) =>
        {
            var result = await svc.GetImprovementSuggestionsAsync(propertyId);
            return Results.Ok(result);
        })
        .WithName("GetAiImprovements")
        .WithOpenApi()
        .RequireAuthorization("ManagerOrAbove")
        .Produces<IEnumerable<AiPromptImprovementDto>>(StatusCodes.Status200OK);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record AiInquiryRequest(
    Guid PropertyId,
    string Question,
    string Source = "WebChat",
    Guid? GuestId = null,
    Guid? BookingId = null,
    string? GuestName = null,
    DateTime? CheckInDate = null,
    DateTime? CheckOutDate = null,
    string? RoomType = null,
    string? LoyaltyTier = null);
