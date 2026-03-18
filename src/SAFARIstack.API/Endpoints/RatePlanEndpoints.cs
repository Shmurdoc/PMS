using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class RatePlanEndpoints
{
    public static void MapRatePlanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rate-plans")
            .WithTags("RatePlans")
            .RequireAuthorization();

        // GET /api/rate-plans/{propertyId} — list all rate plans for a property
        group.MapGet("/{propertyId:guid}", async (Guid propertyId, bool? activeOnly, ApplicationDbContext db) =>
        {
            var query = db.RatePlans
                .Where(rp => rp.PropertyId == propertyId);

            if (activeOnly == true)
                query = query.Where(rp => rp.IsActive);

            var plans = await query
                .OrderBy(rp => rp.Name)
                .Select(rp => new
                {
                    rp.Id,
                    rp.Name,
                    rp.Code,
                    rp.Description,
                    Type = rp.Type.ToString(),
                    rp.IncludesBreakfast,
                    rp.IsRefundable,
                    rp.MinimumNights,
                    rp.MaximumNights,
                    rp.MinimumAdvanceDays,
                    rp.MaximumAdvanceDays,
                    rp.CancellationPolicyId,
                    rp.IsActive,
                    RateCount = rp.Rates.Count
                })
                .ToListAsync();

            return Results.Ok(plans);
        }).WithName("ListRatePlans").WithOpenApi();

        // GET /api/rate-plans/detail/{id} — get rate plan with rates
        group.MapGet("/detail/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var plan = await db.RatePlans
                .Include(rp => rp.CancellationPolicy)
                .Include(rp => rp.Rates)
                .FirstOrDefaultAsync(rp => rp.Id == id);

            if (plan is null) return Results.NotFound();

            return Results.Ok(new
            {
                plan.Id,
                plan.Name,
                plan.Code,
                plan.Description,
                Type = plan.Type.ToString(),
                plan.IncludesBreakfast,
                plan.IsRefundable,
                plan.MinimumNights,
                plan.MaximumNights,
                plan.MinimumAdvanceDays,
                plan.MaximumAdvanceDays,
                plan.IsActive,
                CancellationPolicy = plan.CancellationPolicy != null
                    ? new { plan.CancellationPolicy.Id, plan.CancellationPolicy.Name, plan.CancellationPolicy.FreeCancellationHours }
                    : null,
                Rates = plan.Rates.Select(r => new
                {
                    r.Id,
                    r.RoomTypeId,
                    r.AmountPerNight,
                    r.EffectiveFrom,
                    r.EffectiveTo
                })
            });
        }).WithName("GetRatePlanDetail").WithOpenApi();

        // POST /api/rate-plans — create rate plan
        group.MapPost("/", async (CreateRatePlanRequest req, ApplicationDbContext db) =>
        {
            var plan = RatePlan.Create(
                req.PropertyId,
                req.Name,
                req.Code,
                req.Type,
                req.IncludesBreakfast,
                req.IsRefundable);

            if (req.MinimumNights.HasValue || req.MaximumNights.HasValue ||
                req.MinimumAdvanceDays.HasValue || req.MaximumAdvanceDays.HasValue)
            {
                plan.SetRestrictions(req.MinimumNights, req.MaximumNights,
                    req.MinimumAdvanceDays, req.MaximumAdvanceDays);
            }

            if (req.CancellationPolicyId.HasValue)
            {
                var propType = plan.GetType();
                propType.GetProperty("CancellationPolicyId")!.SetValue(plan, req.CancellationPolicyId.Value);
            }

            await db.RatePlans.AddAsync(plan);
            await db.SaveChangesAsync();

            return Results.Created($"/api/rate-plans/detail/{plan.Id}",
                new { plan.Id, plan.Name, plan.Code, Type = plan.Type.ToString() });
        }).WithName("CreateRatePlan").WithOpenApi();

        // PUT /api/rate-plans/{id} — update rate plan restrictions and metadata
        group.MapPut("/{id:guid}", async (Guid id, UpdateRatePlanRequest req, ApplicationDbContext db) =>
        {
            var plan = await db.RatePlans.FindAsync(id);
            if (plan is null) return Results.NotFound();

            var type = plan.GetType();
            if (req.Name is not null) type.GetProperty("Name")!.SetValue(plan, req.Name);
            if (req.Description is not null) type.GetProperty("Description")!.SetValue(plan, req.Description);
            if (req.IncludesBreakfast.HasValue) type.GetProperty("IncludesBreakfast")!.SetValue(plan, req.IncludesBreakfast.Value);
            if (req.IsRefundable.HasValue) type.GetProperty("IsRefundable")!.SetValue(plan, req.IsRefundable.Value);
            if (req.CancellationPolicyId.HasValue) type.GetProperty("CancellationPolicyId")!.SetValue(plan, req.CancellationPolicyId.Value);

            plan.SetRestrictions(
                req.MinimumNights ?? plan.MinimumNights,
                req.MaximumNights ?? plan.MaximumNights,
                req.MinimumAdvanceDays ?? plan.MinimumAdvanceDays,
                req.MaximumAdvanceDays ?? plan.MaximumAdvanceDays);

            await db.SaveChangesAsync();
            return Results.Ok(new { plan.Id, plan.Name, Message = "Rate plan updated" });
        }).WithName("UpdateRatePlan").WithOpenApi();

        // PUT /api/rate-plans/{id}/activate — reactivate a rate plan
        group.MapPut("/{id:guid}/activate", async (Guid id, ApplicationDbContext db) =>
        {
            var plan = await db.RatePlans.FindAsync(id);
            if (plan is null) return Results.NotFound();

            // Entity has no Activate method, set directly
            plan.GetType().GetProperty("IsActive")!.SetValue(plan, true);
            await db.SaveChangesAsync();
            return Results.Ok(new { plan.Id, plan.Name, IsActive = true });
        }).WithName("ActivateRatePlan").WithOpenApi();

        // PUT /api/rate-plans/{id}/deactivate — deactivate a rate plan 
        group.MapPut("/{id:guid}/deactivate", async (Guid id, ApplicationDbContext db) =>
        {
            var plan = await db.RatePlans.FindAsync(id);
            if (plan is null) return Results.NotFound();

            plan.Deactivate();
            await db.SaveChangesAsync();
            return Results.Ok(new { plan.Id, plan.Name, IsActive = false });
        }).WithName("DeactivateRatePlan").WithOpenApi();
    }
}

public record CreateRatePlanRequest(
    Guid PropertyId, string Name, string Code, RatePlanType Type,
    bool IncludesBreakfast = false, bool IsRefundable = true,
    int? MinimumNights = null, int? MaximumNights = null,
    int? MinimumAdvanceDays = null, int? MaximumAdvanceDays = null,
    Guid? CancellationPolicyId = null);

public record UpdateRatePlanRequest(
    string? Name = null, string? Description = null,
    bool? IncludesBreakfast = null, bool? IsRefundable = null,
    int? MinimumNights = null, int? MaximumNights = null,
    int? MinimumAdvanceDays = null, int? MaximumAdvanceDays = null,
    Guid? CancellationPolicyId = null);
