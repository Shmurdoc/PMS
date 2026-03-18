using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class CancellationPolicyEndpoints
{
    public static void MapCancellationPolicyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cancellation-policies")
            .WithTags("CancellationPolicies")
            .RequireAuthorization();

        // GET /api/cancellation-policies/{propertyId} — list all policies for a property
        group.MapGet("/{propertyId:guid}", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var policies = await db.CancellationPolicies
                .Where(p => p.PropertyId == propertyId)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.FreeCancellationHours,
                    PenaltyPercentage = p.PenaltyPercentage * 100,
                    NoShowPenaltyPercentage = p.NoShowPenaltyPercentage.HasValue
                        ? p.NoShowPenaltyPercentage.Value * 100
                        : (decimal?)null,
                    p.IsDefault
                })
                .ToListAsync();

            return Results.Ok(policies);
        }).WithName("ListCancellationPolicies").WithOpenApi();

        // GET /api/cancellation-policies/detail/{id} — get policy detail
        group.MapGet("/detail/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var policy = await db.CancellationPolicies.FindAsync(id);
            if (policy is null) return Results.NotFound();

            var linkedPlans = await db.RatePlans
                .Where(rp => rp.CancellationPolicyId == id && rp.IsActive)
                .Select(rp => new { rp.Id, rp.Name, rp.Code })
                .ToListAsync();

            return Results.Ok(new
            {
                policy.Id,
                policy.Name,
                policy.Description,
                policy.FreeCancellationHours,
                PenaltyPercentage = policy.PenaltyPercentage * 100,
                NoShowPenaltyPercentage = policy.NoShowPenaltyPercentage.HasValue
                    ? policy.NoShowPenaltyPercentage.Value * 100
                    : (decimal?)null,
                policy.IsDefault,
                LinkedRatePlans = linkedPlans
            });
        }).WithName("GetCancellationPolicy").WithOpenApi();

        // POST /api/cancellation-policies — create a new policy
        group.MapPost("/", async (CreateCancellationPolicyRequest req, ApplicationDbContext db) =>
        {
            var policy = CancellationPolicy.Create(
                req.PropertyId,
                req.Name,
                req.FreeCancellationHours,
                req.PenaltyPercentage / 100m,
                req.IsDefault);

            await db.CancellationPolicies.AddAsync(policy);

            // If this is default, unset other defaults
            if (req.IsDefault)
            {
                var others = await db.CancellationPolicies
                    .Where(p => p.PropertyId == req.PropertyId && p.IsDefault && p.Id != policy.Id)
                    .ToListAsync();
                foreach (var o in others)
                    o.GetType().GetProperty("IsDefault")!.SetValue(o, false);
            }

            await db.SaveChangesAsync();
            return Results.Created($"/api/cancellation-policies/detail/{policy.Id}",
                new { policy.Id, policy.Name });
        }).WithName("CreateCancellationPolicy").WithOpenApi();

        // PUT /api/cancellation-policies/{id} — update policy (direct property set since no Update method)
        group.MapPut("/{id:guid}", async (Guid id, UpdateCancellationPolicyRequest req, ApplicationDbContext db) =>
        {
            var policy = await db.CancellationPolicies.FindAsync(id);
            if (policy is null) return Results.NotFound();

            // Use reflection to update properties (entity has no Update method)
            var type = policy.GetType();
            if (req.Name is not null) type.GetProperty("Name")!.SetValue(policy, req.Name);
            if (req.Description is not null) type.GetProperty("Description")!.SetValue(policy, req.Description);
            if (req.FreeCancellationHours.HasValue)
                type.GetProperty("FreeCancellationHours")!.SetValue(policy, req.FreeCancellationHours.Value);
            if (req.PenaltyPercentage.HasValue)
                type.GetProperty("PenaltyPercentage")!.SetValue(policy, req.PenaltyPercentage.Value / 100m);

            await db.SaveChangesAsync();
            return Results.Ok(new { policy.Id, policy.Name, Message = "Policy updated" });
        }).WithName("UpdateCancellationPolicy").WithOpenApi();

        // DELETE /api/cancellation-policies/{id} — delete policy
        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var policy = await db.CancellationPolicies.FindAsync(id);
            if (policy is null) return Results.NotFound();

            // Don't delete if linked to active rate plans
            var linkedCount = await db.RatePlans
                .CountAsync(rp => rp.CancellationPolicyId == id && rp.IsActive);
            if (linkedCount > 0)
                return Results.BadRequest(new { Error = $"Policy is linked to {linkedCount} active rate plan(s). Remove linkage first." });

            db.CancellationPolicies.Remove(policy);
            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Policy deleted" });
        }).WithName("DeleteCancellationPolicy").WithOpenApi();
    }
}

public record CreateCancellationPolicyRequest(Guid PropertyId, string Name, int FreeCancellationHours, decimal PenaltyPercentage, bool IsDefault = false);
public record UpdateCancellationPolicyRequest(string? Name, string? Description, int? FreeCancellationHours, decimal? PenaltyPercentage);
