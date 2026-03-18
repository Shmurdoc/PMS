using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Amenity CRUD endpoints — manage room and property amenities catalogue.
/// </summary>
public static class AmenityEndpoints
{
    public static void MapAmenityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/amenities")
            .WithTags("Amenities")
            .RequireAuthorization();

        // GET /api/amenities?propertyId={pid} — list amenities for a property
        group.MapGet("/", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var amenities = await db.Amenities
                .Where(a => a.PropertyId == propertyId)
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .Select(a => new
                {
                    a.Id, a.Name, a.Icon, a.PropertyId
                }).ToListAsync();
            return Results.Ok(amenities);
        }).WithName("ListAmenities").WithOpenApi();

        // GET /api/amenities/{id} — get single amenity
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var amenity = await db.Amenities.AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new { a.Id, a.Name, a.Icon, a.PropertyId })
                .FirstOrDefaultAsync();
            return amenity is null ? Results.NotFound() : Results.Ok(amenity);
        }).WithName("GetAmenity").WithOpenApi();

        // POST /api/amenities — create amenity
        group.MapPost("/", async (CreateAmenityRequest req, ApplicationDbContext db) =>
        {
            var amenity = Amenity.Create(req.PropertyId, req.Name, req.Category, req.Icon);
            await db.Amenities.AddAsync(amenity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/amenities/{amenity.Id}",
                new { amenity.Id, amenity.Name, amenity.Icon, Category = amenity.Category.ToString() });
        }).WithName("CreateAmenity").WithOpenApi();

        // DELETE /api/amenities/{id} — remove amenity
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var amenity = await db.Amenities.FindAsync(id);
            if (amenity is null) return Results.NotFound();
            db.Amenities.Remove(amenity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteAmenity").WithOpenApi();
    }
}

public record CreateAmenityRequest(Guid PropertyId, string Name, AmenityCategory Category = AmenityCategory.RoomBasic, string? Icon = null);
