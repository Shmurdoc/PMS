using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class UpsellEndpoints
{
    public static void MapUpsellEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/upsells")
            .WithTags("Upsells")
            .RequireAuthorization();

        // Get personalized offers for a booking
        group.MapGet("/booking/{bookingId:guid}/offers", async (
            Guid bookingId,
            [FromQuery] Guid guestId,
            [FromQuery] Guid propertyId,
            IUpsellEngine engine) =>
        {
            var offers = await engine.GeneratePersonalizedOffersAsync(bookingId, guestId, propertyId);
            return Results.Ok(offers);
        })
        .WithName("GetPersonalizedUpsellOffers")
        .WithOpenApi()
        .Produces<IEnumerable<UpsellOfferDto>>(StatusCodes.Status200OK);

        // Purchase upsell
        group.MapPost("/purchase", async (
            UpsellPurchaseRequest request,
            IUpsellEngine engine) =>
        {
            var result = await engine.PurchaseUpsellAsync(
                request.OfferId, request.BookingId, request.GuestId, request.Quantity);
            return result.Success
                ? Results.Created($"/api/upsells/transactions/{result.TransactionId}", result)
                : Results.BadRequest(result);
        })
        .WithName("PurchaseUpsell")
        .WithOpenApi()
        .Produces<UpsellPurchaseResultDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Upsell analytics
        group.MapGet("/analytics/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            IUpsellEngine engine) =>
        {
            var result = await engine.GetUpsellAnalyticsAsync(propertyId, startDate, endDate);
            return Results.Ok(result);
        })
        .WithName("GetUpsellAnalytics")
        .WithOpenApi()
        .Produces<UpsellAnalyticsDto>(StatusCodes.Status200OK);

        // CRUD for upsell offers (admin)
        group.MapPost("/offers", async (
            CreateUpsellOfferRequest request,
            SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var offer = SAFARIstack.Core.Domain.Entities.UpsellOffer.Create(
                request.PropertyId, request.OfferType,
                request.Title, request.OriginalPrice, request.OfferPrice,
                request.Description, request.CostPrice, request.ImageUrl,
                request.InventoryTotal, request.ValidFrom, request.ValidTo);

            if (request.MinNights.HasValue || request.MinLoyaltyTier != null ||
                request.GuestType != null || request.BookingSource != null)
            {
                offer.SetTargeting(
                    request.MinNights, request.MinLoyaltyTier,
                    request.GuestType, request.BookingSource,
                    request.ApplicableDays, request.MaxDaysBeforeArrival);
            }

            await db.Set<SAFARIstack.Core.Domain.Entities.UpsellOffer>().AddAsync(offer);
            await db.SaveChangesAsync();

            return Results.Created($"/api/upsells/offers/{offer.Id}", new { offer.Id, offer.Title });
        })
        .WithName("CreateUpsellOffer")
        .WithOpenApi()
        .RequireAuthorization("ManagerOrAbove")
        .Produces(StatusCodes.Status201Created);

        group.MapGet("/offers/{propertyId:guid}", async (
            Guid propertyId,
            SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var offers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(
                    db.Set<SAFARIstack.Core.Domain.Entities.UpsellOffer>()
                        .Where(o => o.PropertyId == propertyId)
                        .OrderByDescending(o => o.CreatedAt));

            return Results.Ok(offers.Select(o => new UpsellOfferDto(
                o.Id, o.Title, o.Description,
                o.OfferType.ToString(), o.OriginalPrice, o.OfferPrice,
                o.Savings, o.ImageUrl, o.ValidTo)));
        })
        .WithName("GetUpsellOffersByProperty")
        .WithOpenApi()
        .Produces<IEnumerable<UpsellOfferDto>>(StatusCodes.Status200OK);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record UpsellPurchaseRequest(Guid OfferId, Guid BookingId, Guid GuestId, int Quantity = 1);

public record CreateUpsellOfferRequest(
    Guid PropertyId,
    SAFARIstack.Core.Domain.Entities.UpsellOfferType OfferType,
    string Title,
    decimal OriginalPrice,
    decimal OfferPrice,
    string? Description = null,
    decimal CostPrice = 0,
    string? ImageUrl = null,
    int? InventoryTotal = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    int? MinNights = null,
    string? MinLoyaltyTier = null,
    string? GuestType = null,
    string? BookingSource = null,
    string? ApplicableDays = null,
    int? MaxDaysBeforeArrival = null);
