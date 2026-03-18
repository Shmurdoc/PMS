using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class MultiPropertyEndpoints
{
    public static void MapMultiPropertyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/multi-property")
            .WithTags("Multi-Property")
            .RequireAuthorization("AdminOnly");

        // ─── Property Group CRUD ─────────────────────────────────────
        group.MapGet("/groups/{groupId:guid}/dashboard", async (
            Guid groupId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            IMultiPropertyService svc) =>
        {
            var result = await svc.GetGroupDashboardAsync(groupId, startDate, endDate);
            return Results.Ok(result);
        })
        .WithName("GetGroupDashboard")
        .WithOpenApi()
        .Produces<GroupDashboardDto>(StatusCodes.Status200OK);

        group.MapPost("/groups/{groupId:guid}/rates/copy", async (
            Guid groupId,
            RateCopyRequest request,
            IMultiPropertyService svc) =>
        {
            var result = await svc.CopyRatesAcrossPropertiesAsync(
                request.SourcePropertyId, request.TargetPropertyIds,
                new RateCopyOptionsDto(
                    request.CopyRatePlans, request.CopySeasons, request.CopyRestrictions,
                    request.RateAdjustmentPercentage, request.OverrideExisting,
                    request.EffectiveFrom, request.EffectiveTo));

            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("CopyRatesAcrossProperties")
        .WithOpenApi()
        .Produces<RateCopyResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/groups/{groupId:guid}/inventory/allocate", async (
            Guid groupId,
            GroupInventoryRequestDto request,
            IMultiPropertyService svc) =>
        {
            var result = await svc.AllocateGroupInventoryAsync(groupId, request);
            return result.Success
                ? Results.Created($"/api/multi-property/allocations/{result.AllocationId}", result)
                : Results.BadRequest(result);
        })
        .WithName("AllocateGroupInventory")
        .WithOpenApi()
        .Produces<GroupAllocationResultDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/groups/{groupId:guid}/comparison", async (
            Guid groupId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            IMultiPropertyService svc) =>
        {
            var result = await svc.GetPropertyComparisonAsync(groupId, startDate, endDate);
            return Results.Ok(result);
        })
        .WithName("GetPropertyComparison")
        .WithOpenApi()
        .Produces<IEnumerable<PropertyComparisonDto>>(StatusCodes.Status200OK);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record RateCopyRequest(
    Guid SourcePropertyId,
    IEnumerable<Guid> TargetPropertyIds,
    bool CopyRatePlans = true,
    bool CopySeasons = true,
    bool CopyRestrictions = true,
    decimal RateAdjustmentPercentage = 0,
    bool OverrideExisting = false,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null);
