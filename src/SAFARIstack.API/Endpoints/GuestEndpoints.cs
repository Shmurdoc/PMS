using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class GuestEndpoints
{
    public static void MapGuestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/guests")
            .WithTags("Guests")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        group.MapGet("/search/{propertyId:guid}", async (
            Guid propertyId, string q, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Guests
                .Where(g => g.PropertyId == propertyId &&
                    (g.FirstName.Contains(q) || g.LastName.Contains(q) ||
                     (g.Email != null && g.Email.Contains(q)) ||
                     (g.Phone != null && g.Phone.Contains(q))))
                .AsNoTracking()
                .OrderBy(g => g.LastName)
                .Select(g => new
                {
                    g.Id, g.FirstName, g.LastName, FullName = g.FirstName + " " + g.LastName,
                    g.Email, g.Phone, g.GuestType
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("SearchGuests").WithOpenApi();

        group.MapGet("/{id:guid}", async (Guid id, IUnitOfWork uow) =>
        {
            var guest = await uow.Guests.GetWithPreferencesAsync(id);
            return guest is null ? Results.NotFound() : Results.Ok(guest);
        })
        .WithName("GetGuestById").WithOpenApi();

        group.MapPost("/", async (CreateGuestRequest req, IUnitOfWork uow) =>
        {
            var guest = Guest.Create(req.PropertyId, req.FirstName, req.LastName, req.Email, req.Phone);
            if (!string.IsNullOrEmpty(req.IdNumber))
                guest.UpdateIdInfo(req.IdNumber!, req.IdType ?? IdType.SAId, req.DateOfBirth);
            if (!string.IsNullOrEmpty(req.CompanyName))
                guest.SetCompanyInfo(req.CompanyName!, req.CompanyVATNumber);

            await uow.Guests.AddAsync(guest);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/guests/{guest.Id}", new { guest.Id, guest.FullName });
        })
        .WithName("CreateGuest").WithOpenApi();

        group.MapPut("/{id:guid}/contact", async (Guid id, UpdateContactRequest req, IUnitOfWork uow) =>
        {
            var guest = await uow.Guests.GetByIdAsync(id);
            if (guest is null) return Results.NotFound();
            guest.UpdateContactInfo(req.Email, req.Phone);
            uow.Guests.Update(guest);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateGuestContact").WithOpenApi();

        group.MapPost("/{id:guid}/blacklist", async (Guid id, BlacklistRequest req, IUnitOfWork uow) =>
        {
            var guest = await uow.Guests.GetByIdAsync(id);
            if (guest is null) return Results.NotFound();
            guest.Blacklist(req.Reason);
            uow.Guests.Update(guest);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("BlacklistGuest").WithOpenApi();

        group.MapGet("/{propertyId:guid}/returning", async (Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Guests
                .Include(g => g.Loyalty)
                .Where(g => g.PropertyId == propertyId && g.Loyalty != null && g.Loyalty.TotalStays >= 2)
                .AsNoTracking()
                .OrderByDescending(g => g.Loyalty!.TotalStays)
                .Select(g => new
                {
                    g.Id, FullName = g.FirstName + " " + g.LastName,
                    Tier = g.Loyalty != null ? g.Loyalty.Tier : default,
                    TotalStays = g.Loyalty != null ? g.Loyalty.TotalStays : 0,
                    TotalNights = g.Loyalty != null ? g.Loyalty.TotalNights : 0
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetReturningGuests").WithOpenApi();
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record CreateGuestRequest(
    Guid PropertyId, string FirstName, string LastName,
    string? Email, string? Phone, string? IdNumber,
    IdType? IdType, DateTime? DateOfBirth,
    string? CompanyName, string? CompanyVATNumber);

public record UpdateContactRequest(string? Email, string? Phone);
public record BlacklistRequest(string Reason);
