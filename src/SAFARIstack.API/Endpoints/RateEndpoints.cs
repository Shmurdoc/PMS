using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SAFARIstack.API.Endpoints;

public static class RateEndpoints
{
    public static void MapRateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rates")
            .WithTags("Rates & Pricing")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // ─── Seasons ────────────────────────────────────────────────
        group.MapPost("/seasons", async (CreateSeasonRequest req, ApplicationDbContext db) =>
        {
            var season = Season.Create(req.PropertyId, req.Name, req.Code, req.Type,
                req.StartDate, req.EndDate, req.PriceMultiplier, req.Priority);
            await db.Seasons.AddAsync(season);
            await db.SaveChangesAsync();
            return Results.Created($"/api/rates/seasons/{season.Id}", new
            {
                season.Id, season.Name, season.Code, season.Type,
                season.StartDate, season.EndDate, season.PriceMultiplier
            });
        })
        .WithName("CreateSeason").WithOpenApi();

        group.MapGet("/seasons/{propertyId:guid}", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var seasons = await db.Seasons
                .Where(s => s.PropertyId == propertyId && s.IsActive)
                .OrderBy(s => s.StartDate)
                .Select(s => new
                {
                    s.Id, s.Name, s.Code, s.Type,
                    s.StartDate, s.EndDate, s.PriceMultiplier, s.Priority
                })
                .ToListAsync();
            return Results.Ok(seasons);
        })
        .WithName("GetSeasonsByProperty").WithOpenApi();

        // ─── Rate Plans ─────────────────────────────────────────────
        group.MapGet("/plans/{propertyId:guid}", async (Guid propertyId, IUnitOfWork uow) =>
        {
            // Use specification to get active rate plans
            var spec = new ActiveRatePlansByPropertySpec(propertyId);
            var plans = await uow.Rates.ListAsync(spec);
            return Results.Ok(plans);
        })
        .WithName("GetRatePlans").WithOpenApi();

        // ─── Rates ──────────────────────────────────────────────────
        group.MapGet("/effective", async (
            Guid roomTypeId, Guid ratePlanId, DateTime date, IUnitOfWork uow) =>
        {
            var rate = await uow.Rates.GetEffectiveRateAsync(roomTypeId, ratePlanId, date);
            return rate is null
                ? Results.NotFound("No effective rate found for the given criteria.")
                : Results.Ok(new
                {
                    rate.Id, rate.AmountPerNight, rate.SingleOccupancyRate,
                    rate.ExtraAdultRate, rate.ExtraChildRate,
                    rate.EffectiveFrom, rate.EffectiveTo, rate.Currency
                });
        })
        .WithName("GetEffectiveRate").WithOpenApi();

        group.MapGet("/room-type/{roomTypeId:guid}", async (
            Guid roomTypeId, DateTime from, DateTime to, IUnitOfWork uow) =>
        {
            var rates = await uow.Rates.GetRatesByRoomTypeAsync(roomTypeId, from, to);
            return Results.Ok(rates.Select(r => new
            {
                r.Id, r.AmountPerNight, r.RatePlanId,
                r.EffectiveFrom, r.EffectiveTo, r.IsActive
            }));
        })
        .WithName("GetRatesByRoomType").WithOpenApi();
    }
}

// ─── Specification for active rate plans ────────────────────────────
public class ActiveRatePlansByPropertySpec : SAFARIstack.Core.Domain.Interfaces.Specification<Rate>
{
    public ActiveRatePlansByPropertySpec(Guid propertyId)
    {
        Criteria = r => r.PropertyId == propertyId && r.IsActive;
        ApplyOrderBy(r => r.AmountPerNight);
    }
}

public record CreateSeasonRequest(
    Guid PropertyId, string Name, string Code, SeasonType Type,
    DateTime StartDate, DateTime EndDate, decimal PriceMultiplier, int Priority = 0);
