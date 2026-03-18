using MediatR;
using Microsoft.AspNetCore.Authorization;
using SAFARIstack.Infrastructure.Authentication;
using SAFARIstack.Modules.Staff.Application.Attendance.Commands;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// RFID hardware endpoints - uses X-Reader-API-Key authentication
/// </summary>
public static class RfidEndpoints
{
    public static void MapRfidEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rfid")
            .WithTags("RFID Hardware")
            .RequireAuthorization(RfidReaderAuthenticationOptions.SchemeName)
            .WithAutoValidation();

        // RFID Check-in (called by RFID reader hardware)
        group.MapPost("/check-in", async (RfidCheckInRequest request, IMediator mediator, HttpContext context) =>
        {
            var apiKey = context.Request.Headers["X-Reader-API-Key"].FirstOrDefault();

            var command = new RfidCheckInCommand(
                request.CardUid,
                request.ReaderId,
                apiKey);

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(result);
            }

            return Results.Ok(result);
        })
        .WithName("RfidCheckIn")
        .WithOpenApi()
        .Produces<RfidCheckInResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // RFID Check-out (called by RFID reader hardware)
        group.MapPost("/check-out", async (RfidCheckOutRequest request, IMediator mediator, HttpContext context) =>
        {
            var apiKey = context.Request.Headers["X-Reader-API-Key"].FirstOrDefault();

            var command = new RfidCheckOutCommand(
                request.CardUid,
                request.ReaderId,
                apiKey);

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(result);
            }

            return Results.Ok(result);
        })
        .WithName("RfidCheckOut")
        .WithOpenApi()
        .Produces<RfidCheckOutResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // Reader heartbeat (RFID reader reports status)
        group.MapPost("/heartbeat", async (
            RfidHeartbeatRequest request, SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var reader = await db.RfidReaders.FindAsync(request.ReaderId);
            if (reader is not null)
            {
                reader.RecordHeartbeat();
                await db.SaveChangesAsync();
            }

            return Results.Ok(new
            {
                Status = "OK",
                Timestamp = DateTime.UtcNow,
                ReaderId = request.ReaderId,
                Acknowledged = reader is not null
            });
        })
        .WithName("RfidHeartbeat")
        .WithOpenApi();
    }
}

// Request DTOs
public record RfidCheckInRequest(string CardUid, Guid? ReaderId);
public record RfidCheckOutRequest(string CardUid, Guid? ReaderId);
public record RfidHeartbeatRequest(Guid ReaderId, string ReaderSerial, string Status);
