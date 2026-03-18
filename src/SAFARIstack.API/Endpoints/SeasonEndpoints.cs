using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class SeasonEndpoints
{
    public static void MapSeasonEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/seasons")
            .WithTags("Seasons")
            .RequireAuthorization();

        // GET /api/seasons/{propertyId} — list all seasons for a property
        group.MapGet("/{propertyId:guid}", async (Guid propertyId, bool? activeOnly, ApplicationDbContext db) =>
        {
            var query = db.Seasons
                .Where(s => s.PropertyId == propertyId);

            if (activeOnly == true)
                query = query.Where(s => s.IsActive);

            var seasons = await query
                .OrderBy(s => s.StartDate)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    Type = s.Type.ToString(),
                    s.StartDate,
                    s.EndDate,
                    s.PriceMultiplier,
                    s.Priority,
                    s.IsActive,
                    IsCurrent = s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow
                })
                .ToListAsync();

            return Results.Ok(seasons);
        }).WithName("ListSeasons").WithOpenApi();

        // GET /api/seasons/detail/{id} — get season detail
        group.MapGet("/detail/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var season = await db.Seasons.FindAsync(id);
            if (season is null) return Results.NotFound();

            return Results.Ok(new
            {
                season.Id,
                season.Name,
                season.Code,
                Type = season.Type.ToString(),
                season.StartDate,
                season.EndDate,
                season.PriceMultiplier,
                season.Priority,
                season.IsActive,
                DurationDays = (season.EndDate - season.StartDate).Days,
                IsCurrent = season.CoversDate(DateTime.UtcNow)
            });
        }).WithName("GetSeasonDetail").WithOpenApi();

        // POST /api/seasons — create a new season (also available at /api/rates/seasons)
        group.MapPost("/", async (CreateSeasonRequest req, ApplicationDbContext db) =>
        {
            // Validate no overlap with existing seasons of same type
            var overlap = await db.Seasons
                .AnyAsync(s => s.PropertyId == req.PropertyId
                    && s.IsActive
                    && s.StartDate < req.EndDate
                    && s.EndDate > req.StartDate
                    && s.Type == req.Type);

            if (overlap)
                return Results.BadRequest(new { Error = "Date range overlaps with an existing season of the same type." });

            var season = Season.Create(
                req.PropertyId, req.Name, req.Code, req.Type,
                req.StartDate, req.EndDate, req.PriceMultiplier, req.Priority);

            await db.Seasons.AddAsync(season);
            await db.SaveChangesAsync();

            return Results.Created($"/api/seasons/detail/{season.Id}",
                new { season.Id, season.Name, season.Code, Type = season.Type.ToString() });
        }).WithName("CreateSeasonV2").WithOpenApi();

        // PUT /api/seasons/{id} — update season (direct property set since entity lacks Update method)
        group.MapPut("/{id:guid}", async (Guid id, UpdateSeasonRequest req, ApplicationDbContext db) =>
        {
            var season = await db.Seasons.FindAsync(id);
            if (season is null) return Results.NotFound();

            var type = season.GetType();
            if (req.Name is not null) type.GetProperty("Name")!.SetValue(season, req.Name);
            if (req.StartDate.HasValue) type.GetProperty("StartDate")!.SetValue(season, req.StartDate.Value);
            if (req.EndDate.HasValue) type.GetProperty("EndDate")!.SetValue(season, req.EndDate.Value);
            if (req.PriceMultiplier.HasValue) type.GetProperty("PriceMultiplier")!.SetValue(season, req.PriceMultiplier.Value);
            if (req.Priority.HasValue) type.GetProperty("Priority")!.SetValue(season, req.Priority.Value);
            if (req.IsActive.HasValue) type.GetProperty("IsActive")!.SetValue(season, req.IsActive.Value);

            await db.SaveChangesAsync();
            return Results.Ok(new { season.Id, season.Name, Message = "Season updated" });
        }).WithName("UpdateSeason").WithOpenApi();

        // DELETE /api/seasons/{id} — deactivate (soft-delete) season
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var season = await db.Seasons.FindAsync(id);
            if (season is null) return Results.NotFound();

            season.GetType().GetProperty("IsActive")!.SetValue(season, false);
            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Season deactivated" });
        }).WithName("DeleteSeason").WithOpenApi();

        // GET /api/seasons/current/{propertyId} — get current active season
        group.MapGet("/current/{propertyId:guid}", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var current = await db.Seasons
                .Where(s => s.PropertyId == propertyId && s.IsActive
                    && s.StartDate <= now && s.EndDate >= now)
                .OrderByDescending(s => s.Priority)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    Type = s.Type.ToString(),
                    s.PriceMultiplier,
                    s.StartDate,
                    s.EndDate
                })
                .FirstOrDefaultAsync();

            return current is not null
                ? Results.Ok(current)
                : Results.Ok(new { Message = "No active season — using base rates" });
        }).WithName("GetCurrentSeason").WithOpenApi();
    }
}

// CreateSeasonRequest is defined in RateEndpoints.cs — reused here

public record UpdateSeasonRequest(
    string? Name = null, DateTime? StartDate = null, DateTime? EndDate = null,
    decimal? PriceMultiplier = null, int? Priority = null, bool? IsActive = null);
