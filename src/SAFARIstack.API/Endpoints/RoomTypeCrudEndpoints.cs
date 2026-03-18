using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class RoomTypeCrudEndpoints
{
    public static void MapRoomTypeCrudEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/room-types")
            .WithTags("RoomTypes")
            .RequireAuthorization();

        // GET /api/room-types/{propertyId} — list room types for a property
        group.MapGet("/{propertyId:guid}", async (Guid propertyId, bool? activeOnly, ApplicationDbContext db) =>
        {
            var query = db.RoomTypes
                .Where(rt => rt.PropertyId == propertyId);

            if (activeOnly == true)
                query = query.Where(rt => rt.IsActive);

            var types = await query
                .OrderBy(rt => rt.SortOrder)
                .ThenBy(rt => rt.Name)
                .Select(rt => new
                {
                    rt.Id,
                    rt.Name,
                    rt.Code,
                    rt.Description,
                    rt.BasePrice,
                    rt.MaxGuests,
                    rt.MaxAdults,
                    rt.MaxChildren,
                    rt.RoomCount,
                    rt.SizeInSquareMeters,
                    rt.BedConfiguration,
                    rt.ViewType,
                    rt.SortOrder,
                    rt.IsActive,
                    ActualRoomCount = rt.Rooms.Count,
                    AmenityCount = rt.Amenities.Count
                })
                .ToListAsync();

            return Results.Ok(types);
        }).WithName("ListRoomTypes").WithOpenApi();

        // GET /api/room-types/detail/{id} — get room type with amenities and rooms
        group.MapGet("/detail/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var rt = await db.RoomTypes
                .Include(r => r.Amenities).ThenInclude(a => a.Amenity)
                .Include(r => r.Rooms)
                .Include(r => r.Rates)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rt is null) return Results.NotFound();

            return Results.Ok(new
            {
                rt.Id,
                rt.Name,
                rt.Code,
                rt.Description,
                rt.BasePrice,
                rt.MaxGuests,
                rt.MaxAdults,
                rt.MaxChildren,
                rt.RoomCount,
                rt.SizeInSquareMeters,
                rt.BedConfiguration,
                rt.ViewType,
                rt.SortOrder,
                rt.IsActive,
                Amenities = rt.Amenities.Select(a => new
                {
                    a.AmenityId,
                    a.Amenity.Name,
                    a.Amenity.Icon,
                    Category = a.Amenity.Category.ToString()
                }),
                Rooms = rt.Rooms.Select(r => new
                {
                    r.Id,
                    r.RoomNumber,
                    r.Floor,
                    Status = r.Status.ToString()
                }),
                Rates = rt.Rates.Select(r => new
                {
                    r.Id,
                    r.RatePlanId,
                    r.AmountPerNight,
                    r.EffectiveFrom,
                    r.EffectiveTo
                })
            });
        }).WithName("GetRoomTypeDetail").WithOpenApi();

        // POST /api/room-types — create room type
        group.MapPost("/", async (CreateRoomTypeRequest req, ApplicationDbContext db) =>
        {
            var rt = RoomType.Create(req.PropertyId, req.Name, req.Code,
                req.BasePrice, req.MaxGuests, req.MaxAdults, req.MaxChildren);

            if (req.Description is not null)
                rt.GetType().GetProperty("Description")!.SetValue(rt, req.Description);
            if (req.SizeInSquareMeters.HasValue)
                rt.GetType().GetProperty("SizeInSquareMeters")!.SetValue(rt, req.SizeInSquareMeters.Value);
            if (req.BedConfiguration is not null)
                rt.GetType().GetProperty("BedConfiguration")!.SetValue(rt, req.BedConfiguration);
            if (req.ViewType is not null)
                rt.GetType().GetProperty("ViewType")!.SetValue(rt, req.ViewType);

            await db.RoomTypes.AddAsync(rt);
            await db.SaveChangesAsync();

            return Results.Created($"/api/room-types/detail/{rt.Id}",
                new { rt.Id, rt.Name, rt.Code, rt.BasePrice });
        }).WithName("CreateRoomType").WithOpenApi();

        // PUT /api/room-types/{id} — update room type details
        group.MapPut("/{id:guid}", async (Guid id, UpdateRoomTypeRequest req, ApplicationDbContext db) =>
        {
            var rt = await db.RoomTypes.FindAsync(id);
            if (rt is null) return Results.NotFound();

            var type = rt.GetType();
            if (req.Name is not null) type.GetProperty("Name")!.SetValue(rt, req.Name);
            if (req.Description is not null) type.GetProperty("Description")!.SetValue(rt, req.Description);
            if (req.BasePrice.HasValue) rt.UpdatePricing(req.BasePrice.Value);
            if (req.MaxGuests.HasValue) type.GetProperty("MaxGuests")!.SetValue(rt, req.MaxGuests.Value);
            if (req.MaxAdults.HasValue) type.GetProperty("MaxAdults")!.SetValue(rt, req.MaxAdults.Value);
            if (req.MaxChildren.HasValue) type.GetProperty("MaxChildren")!.SetValue(rt, req.MaxChildren.Value);
            if (req.SizeInSquareMeters.HasValue) type.GetProperty("SizeInSquareMeters")!.SetValue(rt, req.SizeInSquareMeters.Value);
            if (req.BedConfiguration is not null) type.GetProperty("BedConfiguration")!.SetValue(rt, req.BedConfiguration);
            if (req.ViewType is not null) type.GetProperty("ViewType")!.SetValue(rt, req.ViewType);
            if (req.SortOrder.HasValue) type.GetProperty("SortOrder")!.SetValue(rt, req.SortOrder.Value);

            await db.SaveChangesAsync();
            return Results.Ok(new { rt.Id, rt.Name, Message = "Room type updated" });
        }).WithName("UpdateRoomType").WithOpenApi();

        // DELETE /api/room-types/{id} — soft-delete (deactivate) room type
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var rt = await db.RoomTypes.FindAsync(id);
            if (rt is null) return Results.NotFound();

            // Check if rooms still assigned
            var activeRooms = await db.Rooms
                .CountAsync(r => r.RoomTypeId == id && r.Status != RoomStatus.OutOfService);
            if (activeRooms > 0)
                return Results.BadRequest(new { Error = $"{activeRooms} active room(s) still assigned to this type." });

            rt.Deactivate();
            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Room type deactivated" });
        }).WithName("DeleteRoomType").WithOpenApi();

        // ── Room-Type Amenity Assignment ───────────────────────────

        // POST /api/room-types/{rtId}/amenities/{aId} — assign amenity to room type
        group.MapPost("/{rtId:guid}/amenities/{aId:guid}", async (Guid rtId, Guid aId, ApplicationDbContext db) =>
        {
            var rtExists = await db.RoomTypes.AnyAsync(r => r.Id == rtId);
            if (!rtExists) return Results.NotFound(new { Error = "Room type not found" });

            var amenityExists = await db.Amenities.AnyAsync(a => a.Id == aId);
            if (!amenityExists) return Results.NotFound(new { Error = "Amenity not found" });

            var alreadyLinked = await db.RoomTypeAmenities
                .AnyAsync(rta => rta.RoomTypeId == rtId && rta.AmenityId == aId);
            if (alreadyLinked)
                return Results.BadRequest(new { Error = "Amenity already assigned to this room type" });

            var link = RoomTypeAmenity.Create(rtId, aId);
            await db.RoomTypeAmenities.AddAsync(link);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Amenity assigned", RoomTypeId = rtId, AmenityId = aId });
        }).WithName("AssignRoomTypeAmenity").WithOpenApi();

        // DELETE /api/room-types/{rtId}/amenities/{aId} — remove amenity from room type
        group.MapDelete("/{rtId:guid}/amenities/{aId:guid}", async (Guid rtId, Guid aId, ApplicationDbContext db) =>
        {
            var link = await db.RoomTypeAmenities
                .FirstOrDefaultAsync(rta => rta.RoomTypeId == rtId && rta.AmenityId == aId);
            if (link is null)
                return Results.NotFound(new { Error = "Amenity not assigned to this room type" });

            db.RoomTypeAmenities.Remove(link);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Amenity removed from room type" });
        }).WithName("RemoveRoomTypeAmenity").WithOpenApi();
    }
}

public record CreateRoomTypeRequest(
    Guid PropertyId, string Name, string Code, decimal BasePrice,
    int MaxGuests, int MaxAdults = 2, int MaxChildren = 0,
    string? Description = null, int? SizeInSquareMeters = null,
    string? BedConfiguration = null, string? ViewType = null);

public record UpdateRoomTypeRequest(
    string? Name = null, string? Description = null,
    decimal? BasePrice = null, int? MaxGuests = null,
    int? MaxAdults = null, int? MaxChildren = null,
    int? SizeInSquareMeters = null, string? BedConfiguration = null,
    string? ViewType = null, int? SortOrder = null);
