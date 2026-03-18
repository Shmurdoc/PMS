using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class DigitalCheckInEndpoints
{
    public static void MapDigitalCheckInEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/digital-checkin")
            .WithTags("Digital Check-In")
            .RequireAuthorization();

        // Initiate check-in for booking
        group.MapPost("/initiate/{bookingId:guid}", async (
            Guid bookingId,
            IDigitalCheckInService svc) =>
        {
            var result = await svc.InitiateCheckInAsync(bookingId);
            return Results.Created($"/api/digital-checkin/{result.Id}", result);
        })
        .WithName("InitiateDigitalCheckIn")
        .WithOpenApi()
        .Produces<DigitalCheckInDto>(StatusCodes.Status201Created);

        // Verify guest identity (accepts form file)
        group.MapPost("/{checkInId:guid}/verify-identity", async (
            Guid checkInId,
            IFormFile idDocument,
            IDigitalCheckInService svc) =>
        {
            await using var stream = idDocument.OpenReadStream();
            var result = await svc.VerifyIdentityAsync(checkInId, stream);
            return Results.Ok(result);
        })
        .WithName("VerifyIdentity")
        .WithOpenApi()
        .DisableAntiforgery()
        .Produces<DigitalCheckInDto>(StatusCodes.Status200OK);

        // Get eligible rooms for selection
        group.MapGet("/{checkInId:guid}/rooms", async (
            Guid checkInId,
            IDigitalCheckInService svc) =>
        {
            var result = await svc.GetEligibleRoomsAsync(checkInId);
            return Results.Ok(result);
        })
        .WithName("GetEligibleRooms")
        .WithOpenApi()
        .Produces<IEnumerable<AvailableRoomDto>>(StatusCodes.Status200OK);

        // Select a room
        group.MapPost("/{checkInId:guid}/select-room", async (
            Guid checkInId,
            SelectRoomRequest request,
            IDigitalCheckInService svc) =>
        {
            var result = await svc.SelectRoomAsync(checkInId, request.RoomId, request.IsUpgrade, request.UpgradeAmount);
            return Results.Ok(result);
        })
        .WithName("SelectRoom")
        .WithOpenApi()
        .Produces<DigitalCheckInDto>(StatusCodes.Status200OK);

        // Sign registration card
        group.MapPost("/{checkInId:guid}/sign", async (
            Guid checkInId,
            SignRegistrationRequest request,
            IDigitalCheckInService svc) =>
        {
            var result = await svc.SignRegistrationCardAsync(
                checkInId, request.SignatureData, request.IpAddress,
                request.PopiaConsent, request.MarketingConsent);
            return Results.Ok(result);
        })
        .WithName("SignRegistration")
        .WithOpenApi()
        .Produces<DigitalCheckInDto>(StatusCodes.Status200OK);

        // Complete check-in
        group.MapPost("/{checkInId:guid}/complete", async (
            Guid checkInId,
            IDigitalCheckInService svc) =>
        {
            var result = await svc.CompleteCheckInAsync(checkInId);
            return Results.Ok(result);
        })
        .WithName("CompleteDigitalCheckIn")
        .WithOpenApi()
        .Produces<DigitalCheckInDto>(StatusCodes.Status200OK);

        // Get check-in status by booking
        group.MapGet("/booking/{bookingId:guid}", async (
            Guid bookingId,
            SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var checkIn = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(
                    db.DigitalCheckIns.Where(d => d.BookingId == bookingId));

            return checkIn is null
                ? Results.NotFound()
                : Results.Ok(new DigitalCheckInDto(
                    checkIn.Id, checkIn.BookingId, checkIn.Status.ToString(),
                    checkIn.IdVerified, checkIn.SignedAt.HasValue,
                    checkIn.SelectedRoomId, checkIn.MobileKeyStatus.ToString(),
                    checkIn.CompletedAt));
        })
        .WithName("GetDigitalCheckInByBooking")
        .WithOpenApi()
        .Produces<DigitalCheckInDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record SelectRoomRequest(Guid RoomId, bool IsUpgrade = false, decimal UpgradeAmount = 0);
public record SignRegistrationRequest(string SignatureData, string IpAddress, bool PopiaConsent, bool MarketingConsent);
