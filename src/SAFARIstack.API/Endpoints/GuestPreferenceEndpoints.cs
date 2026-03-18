using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class GuestPreferenceEndpoints
{
    public static void MapGuestPreferenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/guests")
            .WithTags("GuestPreferences")
            .RequireAuthorization();

        // GET /api/guests/{guestId}/preferences — get all preferences for a guest
        group.MapGet("/{guestId:guid}/preferences", async (Guid guestId, ApplicationDbContext db) =>
        {
            var guest = await db.Guests.FindAsync(guestId);
            if (guest is null) return Results.NotFound(new { Error = "Guest not found" });

            var prefs = await db.GuestPreferences
                .Where(p => p.GuestId == guestId)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Key)
                .Select(p => new
                {
                    p.Id,
                    Category = p.Category.ToString(),
                    p.Key,
                    p.Value,
                    p.Notes
                })
                .ToListAsync();

            return Results.Ok(new
            {
                GuestId = guestId,
                TotalPreferences = prefs.Count,
                Preferences = prefs
            });
        }).WithName("GetGuestPreferences").WithOpenApi();

        // POST /api/guests/{guestId}/preferences — add a preference
        group.MapPost("/{guestId:guid}/preferences", async (Guid guestId, AddPreferenceRequest req, ApplicationDbContext db) =>
        {
            var guest = await db.Guests.FindAsync(guestId);
            if (guest is null) return Results.NotFound(new { Error = "Guest not found" });

            // Check if preference already exists for this category+key
            var existing = await db.GuestPreferences
                .FirstOrDefaultAsync(p => p.GuestId == guestId
                    && p.Category == req.Category && p.Key == req.Key);

            if (existing is not null)
            {
                existing.Update(req.Value);
                await db.SaveChangesAsync();
                return Results.Ok(new { existing.Id, Message = "Preference updated" });
            }

            var pref = GuestPreference.Create(guestId, req.Category, req.Key, req.Value);
            guest.AddPreference(pref);
            await db.SaveChangesAsync();

            return Results.Created($"/api/guests/{guestId}/preferences",
                new { pref.Id, Category = req.Category.ToString(), req.Key, req.Value });
        }).WithName("AddGuestPreference").WithOpenApi();

        // PUT /api/guests/{guestId}/preferences — batch update preferences
        group.MapPut("/{guestId:guid}/preferences", async (Guid guestId, BatchPreferenceUpdate req, ApplicationDbContext db) =>
        {
            var guest = await db.Guests.FindAsync(guestId);
            if (guest is null) return Results.NotFound(new { Error = "Guest not found" });

            var existingPrefs = await db.GuestPreferences
                .Where(p => p.GuestId == guestId)
                .ToListAsync();

            int updated = 0, created = 0;

            foreach (var item in req.Preferences)
            {
                var existing = existingPrefs.FirstOrDefault(p =>
                    p.Category == item.Category && p.Key == item.Key);

                if (existing is not null)
                {
                    existing.Update(item.Value);
                    updated++;
                }
                else
                {
                    var pref = GuestPreference.Create(guestId, item.Category, item.Key, item.Value);
                    guest.AddPreference(pref);
                    created++;
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Updated = updated, Created = created, Total = updated + created });
        }).WithName("BatchUpdateGuestPreferences").WithOpenApi();

        // DELETE /api/guests/{guestId}/preferences/{prefId} — remove a preference
        group.MapDelete("/{guestId:guid}/preferences/{prefId:guid}", async (Guid guestId, Guid prefId, ApplicationDbContext db) =>
        {
            var pref = await db.GuestPreferences
                .FirstOrDefaultAsync(p => p.Id == prefId && p.GuestId == guestId);
            if (pref is null) return Results.NotFound();

            db.GuestPreferences.Remove(pref);
            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Preference removed" });
        }).WithName("DeleteGuestPreference").WithOpenApi();

        // ── Loyalty ─────────────────────────────────────────────────

        // GET /api/guests/{guestId}/loyalty — get loyalty status
        group.MapGet("/{guestId:guid}/loyalty", async (Guid guestId, ApplicationDbContext db) =>
        {
            var loyalty = await db.Set<GuestLoyalty>()
                .FirstOrDefaultAsync(l => l.GuestId == guestId);

            if (loyalty is null)
                return Results.Ok(new { GuestId = guestId, Enrolled = false, Message = "Not enrolled in loyalty program" });

            return Results.Ok(new
            {
                GuestId = guestId,
                Enrolled = true,
                loyalty.TotalPoints,
                loyalty.AvailablePoints,
                loyalty.TotalStays,
                loyalty.TotalNights,
                loyalty.TotalSpend,
                Tier = loyalty.Tier.ToString()
            });
        }).WithName("GetGuestLoyalty").WithOpenApi();

        // POST /api/loyalty/enroll/{guestId} — enroll guest in loyalty program
        group.MapPost("/loyalty/enroll/{guestId:guid}", async (Guid guestId, ApplicationDbContext db) =>
        {
            var guest = await db.Guests.FindAsync(guestId);
            if (guest is null) return Results.NotFound(new { Error = "Guest not found" });

            var existing = await db.Set<GuestLoyalty>()
                .AnyAsync(l => l.GuestId == guestId);
            if (existing)
                return Results.BadRequest(new { Error = "Guest is already enrolled in the loyalty program" });

            var loyalty = GuestLoyalty.Create(guestId);
            await db.Set<GuestLoyalty>().AddAsync(loyalty);
            await db.SaveChangesAsync();

            return Results.Created($"/api/guests/{guestId}/loyalty",
                new { loyalty.Id, GuestId = guestId, Tier = loyalty.Tier.ToString(), TotalPoints = 0 });
        }).WithName("EnrollGuestLoyalty").WithOpenApi();

        // GET /api/loyalty/points/{guestId} — get points balance
        group.MapGet("/loyalty/points/{guestId:guid}", async (Guid guestId, ApplicationDbContext db) =>
        {
            var loyalty = await db.Set<GuestLoyalty>()
                .FirstOrDefaultAsync(l => l.GuestId == guestId);

            if (loyalty is null)
                return Results.NotFound(new { Error = "Guest not enrolled in loyalty program" });

            return Results.Ok(new
            {
                GuestId = guestId,
                loyalty.TotalPoints,
                loyalty.AvailablePoints,
                Tier = loyalty.Tier.ToString()
            });
        }).WithName("GetLoyaltyPoints").WithOpenApi();

        // POST /api/loyalty/redeem — redeem loyalty points
        group.MapPost("/loyalty/redeem", async (RedeemPointsRequest req, ApplicationDbContext db) =>
        {
            var loyalty = await db.Set<GuestLoyalty>()
                .FirstOrDefaultAsync(l => l.GuestId == req.GuestId);

            if (loyalty is null)
                return Results.NotFound(new { Error = "Guest not enrolled in loyalty program" });

            var success = loyalty.RedeemPoints(req.Points);
            if (!success)
                return Results.BadRequest(new { Error = "Insufficient points", loyalty.AvailablePoints });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                Message = $"{req.Points} points redeemed successfully",
                RemainingPoints = loyalty.AvailablePoints,
                Tier = loyalty.Tier.ToString()
            });
        }).WithName("RedeemLoyaltyPoints").WithOpenApi();
    }
}

public record AddPreferenceRequest(PreferenceCategory Category, string Key, string Value);
public record BatchPreferenceUpdate(List<PreferenceItem> Preferences);
public record PreferenceItem(PreferenceCategory Category, string Key, string Value);
public record RedeemPointsRequest(Guid GuestId, int Points);
