using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class GiftCardEndpoints
{
    public static void MapGiftCardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/gift-cards")
            .WithTags("Gift Cards")
            .RequireAuthorization();

        // Create gift card
        group.MapPost("/", async (
            CreateGiftCardRequestDto request,
            IGiftCardService svc) =>
        {
            var result = await svc.CreateGiftCardAsync(request);
            return Results.Created($"/api/gift-cards/{result.Id}", result);
        })
        .WithName("CreateGiftCard")
        .WithOpenApi()
        .Produces<GiftCardDto>(StatusCodes.Status201Created);

        // Check balance (guest-facing — no auth required for card+PIN)
        app.MapGet("/api/gift-cards/balance", async (
            [FromQuery] string cardNumber,
            [FromQuery] string pin,
            IGiftCardService svc) =>
        {
            try
            {
                var result = await svc.CheckBalanceAsync(cardNumber, pin);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CheckGiftCardBalance")
        .WithTags("Gift Cards")
        .WithOpenApi()
        .AllowAnonymous()
        .Produces<GiftCardBalanceDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Redeem gift card
        group.MapPost("/redeem", async (
            GiftCardRedeemRequest request,
            IGiftCardService svc) =>
        {
            var result = await svc.RedeemAsync(
                request.CardNumber, request.Pin, request.Amount,
                request.PropertyId, request.BookingId, request.FolioId);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("RedeemGiftCard")
        .WithOpenApi()
        .Produces<GiftCardRedemptionResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // List gift cards by property
        group.MapGet("/property/{propertyId:guid}", async (
            Guid propertyId,
            IGiftCardService svc) =>
        {
            var result = await svc.GetGiftCardsByPropertyAsync(propertyId);
            return Results.Ok(result);
        })
        .WithName("GetGiftCardsByProperty")
        .WithOpenApi()
        .Produces<IEnumerable<GiftCardDto>>(StatusCodes.Status200OK);

        // Void gift card
        group.MapPost("/{giftCardId:guid}/void", async (
            Guid giftCardId,
            GiftCardVoidRequest request,
            IGiftCardService svc) =>
        {
            await svc.VoidGiftCardAsync(giftCardId, request.Reason);
            return Results.NoContent();
        })
        .WithName("VoidGiftCard")
        .WithOpenApi()
        .RequireAuthorization("ManagerOrAbove")
        .Produces(StatusCodes.Status204NoContent);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record GiftCardRedeemRequest(
    string CardNumber, string Pin, decimal Amount,
    Guid PropertyId, Guid? BookingId = null, Guid? FolioId = null);

public record GiftCardVoidRequest(string Reason);
