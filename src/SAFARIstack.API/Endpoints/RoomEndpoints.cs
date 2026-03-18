using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("Rooms")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // ─── Rooms ──────────────────────────────────────────────────
        group.MapGet("/available/{propertyId:guid}", async (
            Guid propertyId, DateTime checkIn, DateTime checkOut, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var bookedRoomIds = db.BookingRooms
                .Where(br => br.Booking.PropertyId == propertyId &&
                             br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn &&
                             br.Booking.Status != BookingStatus.Cancelled && br.Booking.Status != BookingStatus.NoShow)
                .Select(br => br.RoomId);

            var blockedRoomIds = db.RoomBlocks
                .Where(rb => rb.PropertyId == propertyId && rb.StartDate < checkOut && rb.EndDate > checkIn)
                .Select(rb => rb.RoomId);

            var query = db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.PropertyId == propertyId && r.IsActive &&
                            r.Status != RoomStatus.OutOfService && r.Status != RoomStatus.Maintenance &&
                            !bookedRoomIds.Contains(r.Id) && !blockedRoomIds.Contains(r.Id))
                .AsNoTracking()
                .OrderBy(r => r.RoomNumber)
                .Select(r => new
                {
                    r.Id, r.RoomNumber, r.Floor, r.Wing, r.Status, r.HkStatus,
                    RoomType = new { r.RoomType.Id, r.RoomType.Name, r.RoomType.Code, r.RoomType.BasePrice, r.RoomType.MaxGuests }
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithName("GetAvailableRooms").WithOpenApi();

        group.MapGet("/status/{propertyId:guid}", async (
            Guid propertyId, RoomStatus status, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Rooms
                .Where(r => r.PropertyId == propertyId && r.Status == status)
                .AsNoTracking()
                .OrderBy(r => r.RoomNumber)
                .Select(r => new { r.Id, r.RoomNumber, r.Status, r.HkStatus, r.Notes });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithName("GetRoomsByStatus").WithOpenApi();

        group.MapGet("/floor/{propertyId:guid}/{floor:int}", async (
            Guid propertyId, int floor, IUnitOfWork uow) =>
        {
            var rooms = await uow.Rooms.GetByFloorAsync(propertyId, floor);
            var projected = rooms.Select(r => new
            {
                r.Id, r.RoomNumber, r.Floor, r.Wing, r.Status, r.HkStatus, r.Notes, r.IsActive
            });
            return Results.Ok(projected);
        })
        .WithName("GetRoomsByFloor").WithOpenApi();

        group.MapPost("/", async (CreateRoomRequest req, IUnitOfWork uow) =>
        {
            var room = Room.Create(req.PropertyId, req.RoomTypeId, req.RoomNumber, req.Floor, req.Wing);
            await uow.Rooms.AddAsync(room);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/rooms/{room.Id}", new { room.Id, room.RoomNumber });
        })
        .WithName("CreateRoom").WithOpenApi();

        group.MapPatch("/{id:guid}/status", async (Guid id, RoomStatus status, IUnitOfWork uow) =>
        {
            var room = await uow.Rooms.GetByIdAsync(id);
            if (room is null) return Results.NotFound();
            room.UpdateStatus(status);
            uow.Rooms.Update(room);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateRoomStatus").WithOpenApi();

        group.MapPost("/{id:guid}/clean", async (Guid id, IUnitOfWork uow) =>
        {
            var room = await uow.Rooms.GetByIdAsync(id);
            if (room is null) return Results.NotFound();
            room.MarkClean(DateTime.UtcNow);
            uow.Rooms.Update(room);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("MarkRoomClean").WithOpenApi();

        group.MapPost("/{id:guid}/dirty", async (Guid id, IUnitOfWork uow) =>
        {
            var room = await uow.Rooms.GetByIdAsync(id);
            if (room is null) return Results.NotFound();
            room.MarkDirty();
            uow.Rooms.Update(room);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("MarkRoomDirty").WithOpenApi();

        // ─── Room Types ─────────────────────────────────────────────
        group.MapGet("/types/{propertyId:guid}", async (Guid propertyId, IUnitOfWork uow) =>
        {
            var types = await uow.RoomTypes.GetByPropertyWithRatesAsync(propertyId);
            return Results.Ok(types.Select(rt => new
            {
                rt.Id, rt.Name, rt.Code, rt.BasePrice, rt.MaxGuests,
                rt.MaxAdults, rt.MaxChildren, rt.BedConfiguration, rt.ViewType,
                rt.SortOrder, rt.IsActive,
                ActiveRates = rt.Rates.Count(r => r.IsActive)
            }));
        })
        .WithName("GetRoomTypes").WithOpenApi();

        group.MapGet("/types/{roomTypeId:guid}/availability", async (
            Guid roomTypeId, DateTime checkIn, DateTime checkOut, IUnitOfWork uow) =>
        {
            var count = await uow.RoomTypes.GetAvailableCountAsync(roomTypeId, checkIn, checkOut);
            return Results.Ok(new { RoomTypeId = roomTypeId, AvailableRooms = count });
        })
        .WithName("GetRoomTypeAvailability").WithOpenApi();

        // ─── Room Blocks ────────────────────────────────────────────
        group.MapPost("/blocks", async (CreateRoomBlockRequest req, IUnitOfWork uow) =>
        {
            var block = RoomBlock.Create(req.PropertyId, req.RoomId, req.StartDate, req.EndDate, req.Reason, req.Notes);
            var repo = uow.Repository<RoomBlock>();
            await repo.AddAsync(block);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/rooms/blocks/{block.Id}", new { block.Id });
        })
        .WithName("CreateRoomBlock").WithOpenApi();
    }
}

public record CreateRoomRequest(Guid PropertyId, Guid RoomTypeId, string RoomNumber, int? Floor, string? Wing);
public record CreateRoomBlockRequest(Guid PropertyId, Guid RoomId, DateTime StartDate, DateTime EndDate, RoomBlockReason Reason, string? Notes);
