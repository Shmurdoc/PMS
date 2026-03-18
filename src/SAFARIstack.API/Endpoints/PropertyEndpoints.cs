using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Property CRUD endpoints — full lifecycle management for lodge/hotel properties.
/// </summary>
public static class PropertyEndpoints
{
    public static void MapPropertyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/properties")
            .WithTags("Properties")
            .RequireAuthorization();

        // GET /api/properties — list all properties (super-admin or filtered by user)
        group.MapGet("/", async (HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            IQueryable<Property> query;

            if (context.User.IsInRole("SuperAdmin"))
            {
                query = db.Properties.AsNoTracking().OrderBy(p => p.Name);
            }
            else if (Guid.TryParse(propertyIdClaim, out var propertyId))
            {
                query = db.Properties.Where(p => p.Id == propertyId).AsNoTracking();
            }
            else
            {
                return Results.Unauthorized();
            }

            var projected = query.Select(p => new
            {
                p.Id, p.Name, p.Slug, p.Description, p.Address, p.City,
                p.Province, p.PostalCode, p.Country, p.Phone, p.Email,
                p.Website, p.CheckInTime, p.CheckOutTime, p.Currency,
                p.VATRate, p.TourismLevyRate, p.Timezone, p.IsActive, p.CreatedAt
            });
            return Results.Ok(await PaginationHelpers.PaginateAsync(projected, page ?? 1, pageSize ?? 25));
        }).WithName("ListProperties").WithOpenApi();

        // GET /api/properties/{id} — get single property
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var property = await db.Properties.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id, p.Name, p.Slug, p.Description, p.Address, p.City,
                    p.Province, p.PostalCode, p.Country, p.Phone, p.Email,
                    p.Website, p.CheckInTime, p.CheckOutTime, p.Currency,
                    p.VATRate, p.TourismLevyRate, p.Timezone, p.IsActive, p.CreatedAt,
                    RoomCount = db.Rooms.Count(r => r.PropertyId == p.Id && r.IsActive),
                    StaffCount = db.StaffMembers.Count(s => s.PropertyId == p.Id && s.IsActive)
                }).FirstOrDefaultAsync();
            return property is null ? Results.NotFound() : Results.Ok(property);
        }).WithName("GetProperty").WithOpenApi();

        // POST /api/properties — create new property
        group.MapPost("/", async (CreatePropertyRequest req, ApplicationDbContext db) =>
        {
            var slug = req.Name.ToLowerInvariant().Replace(" ", "-").Replace("'", "");
            var property = Property.Create(req.Name, slug, req.Address, req.City, req.Province);
            await db.Properties.AddAsync(property);
            await db.SaveChangesAsync();
            return Results.Created($"/api/properties/{property.Id}", new
            {
                property.Id, property.Name, property.Slug, property.CreatedAt
            });
        }).WithName("CreateProperty").WithOpenApi();

        // PUT /api/properties/{id} — update property details
        group.MapPut("/{id:guid}", async (Guid id, UpdatePropertyRequest req, ApplicationDbContext db) =>
        {
            var property = await db.Properties.FindAsync(id);
            if (property is null) return Results.NotFound();
            property.UpdateDetails(req.Name, req.Address, req.City, req.Province);
            await db.SaveChangesAsync();
            return Results.Ok(new { property.Id, property.Name, Message = "Property updated." });
        }).WithName("UpdateProperty").WithOpenApi();

        // POST /api/properties/{id}/deactivate — soft-deactivate property
        group.MapPost("/{id:guid}/deactivate", async (Guid id, ApplicationDbContext db) =>
        {
            var property = await db.Properties.FindAsync(id);
            if (property is null) return Results.NotFound();
            property.Deactivate();
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeactivateProperty").WithOpenApi();

        // GET /api/properties/{id}/summary — operational summary for a property
        group.MapGet("/{id:guid}/summary", async (Guid id, ApplicationDbContext db) =>
        {
            var property = await db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (property is null) return Results.NotFound();
            var today = DateTime.UtcNow.Date;
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == id && r.IsActive);
            var occupied = await db.Rooms.CountAsync(r => r.PropertyId == id && r.Status == RoomStatus.Occupied);
            var activeStaff = await db.StaffMembers.CountAsync(s => s.PropertyId == id && s.IsActive);
            var todayCheckIns = await db.Bookings.CountAsync(b => b.PropertyId == id && b.CheckInDate.Date == today && b.Status == BookingStatus.Confirmed);
            var todayCheckOuts = await db.Bookings.CountAsync(b => b.PropertyId == id && b.CheckOutDate.Date == today && b.Status == BookingStatus.CheckedIn);
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthRevenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == id && p.PaymentDate >= monthStart)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Results.Ok(new
            {
                property.Id, property.Name, property.IsActive,
                TotalRooms = totalRooms, OccupiedRooms = occupied,
                OccupancyRate = totalRooms > 0 ? Math.Round((decimal)occupied / totalRooms * 100, 1) : 0,
                ActiveStaff = activeStaff,
                TodayCheckIns = todayCheckIns, TodayCheckOuts = todayCheckOuts,
                MonthRevenue = monthRevenue
            });
        }).WithName("GetPropertySummary").WithOpenApi();
    }
}

// DTOs for PropertyEndpoints
public record CreatePropertyRequest(string Name, string Address, string City, string Province);
public record UpdatePropertyRequest(string Name, string Address, string City, string Province);
