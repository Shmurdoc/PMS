using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class RoomOperationsEndpoints
{
    public static void MapRoomOperationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("RoomOperations")
            .RequireAuthorization();

        // PUT /api/rooms/{id}/out-of-order — mark room out of order/service
        group.MapPut("/{id:guid}/out-of-order", async (Guid id, OutOfOrderRequest req, ApplicationDbContext db) =>
        {
            var room = await db.Rooms.FindAsync(id);
            if (room is null) return Results.NotFound();

            room.PutOutOfService(req.Reason);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                room.Id,
                room.RoomNumber,
                Status = room.Status.ToString(),
                req.Reason,
                Message = "Room marked out of service"
            });
        }).WithName("MarkRoomOutOfOrder").WithOpenApi();

        // PUT /api/rooms/{id}/back-in-service — restore room to available
        group.MapPut("/{id:guid}/back-in-service", async (Guid id, ApplicationDbContext db) =>
        {
            var room = await db.Rooms.FindAsync(id);
            if (room is null) return Results.NotFound();

            room.UpdateStatus(RoomStatus.Available);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                room.Id,
                room.RoomNumber,
                Status = room.Status.ToString(),
                Message = "Room restored to service"
            });
        }).WithName("RestoreRoomToService").WithOpenApi();

        // PUT /api/rooms/{id} — update room details
        group.MapPut("/{id:guid}", async (Guid id, UpdateRoomRequest req, ApplicationDbContext db) =>
        {
            var room = await db.Rooms.FindAsync(id);
            if (room is null) return Results.NotFound();

            var type = room.GetType();
            if (req.RoomNumber is not null) type.GetProperty("RoomNumber")!.SetValue(room, req.RoomNumber);
            if (req.Floor.HasValue) type.GetProperty("Floor")!.SetValue(room, req.Floor.Value);
            if (req.Wing is not null) type.GetProperty("Wing")!.SetValue(room, req.Wing);
            if (req.RoomTypeId.HasValue) type.GetProperty("RoomTypeId")!.SetValue(room, req.RoomTypeId.Value);

            await db.SaveChangesAsync();
            return Results.Ok(new { room.Id, room.RoomNumber, Message = "Room updated" });
        }).WithName("UpdateRoom").WithOpenApi();

        // DELETE /api/rooms/{id} — deactivate room
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var room = await db.Rooms.FindAsync(id);
            if (room is null) return Results.NotFound();

            // Check no active bookings
            var hasBookings = await db.BookingRooms
                .AnyAsync(br => br.RoomId == id
                    && br.Booking.Status != BookingStatus.Cancelled
                    && br.Booking.CheckOutDate > DateTime.UtcNow);
            if (hasBookings)
                return Results.BadRequest(new { Error = "Room has active or upcoming bookings." });

            room.Deactivate();
            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Room deactivated" });
        }).WithName("DeactivateRoom").WithOpenApi();
    }
}

public record OutOfOrderRequest(string Reason);
public record UpdateRoomRequest(string? RoomNumber = null, int? Floor = null, string? Wing = null, Guid? RoomTypeId = null);
