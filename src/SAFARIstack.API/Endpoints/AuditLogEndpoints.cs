using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Audit log query endpoints — read-only access to system-wide change tracking.
/// </summary>
public static class AuditLogEndpoints
{
    public static void MapAuditLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audit-logs")
            .WithTags("Audit")
            .RequireAuthorization();

        // GET /api/audit-logs — query audit logs with filters
        group.MapGet("/", async (
            HttpContext context,
            Guid? propertyId, Guid? userId, string? entityType,
            string? action, DateTime? from, DateTime? to,
            int? page, int? pageSize,
            ApplicationDbContext db) =>
        {
            var pid = propertyId ?? (Guid.TryParse(context.User.FindFirstValue("propertyId"), out var p) ? p : (Guid?)null);

            var query = db.AuditLogs.AsNoTracking().AsQueryable();

            if (pid.HasValue) query = query.Where(a => a.PropertyId == pid.Value);
            if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
            if (!string.IsNullOrEmpty(entityType)) query = query.Where(a => a.EntityType == entityType);
            if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
            if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);

            var projected = query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id, a.PropertyId, a.UserId, a.UserName,
                    a.Action, a.EntityType, a.EntityId,
                    a.IpAddress, a.CreatedAt
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(projected, page ?? 1, pageSize ?? 50));
        }).WithName("QueryAuditLogs").WithOpenApi();

        // GET /api/audit-logs/{id} — get audit log detail with old/new values
        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var log = await db.AuditLogs.AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.Id, a.PropertyId, a.UserId, a.UserName,
                    a.Action, a.EntityType, a.EntityId,
                    a.OldValues, a.NewValues,
                    a.IpAddress, a.UserAgent, a.CreatedAt
                }).FirstOrDefaultAsync();
            return log is null ? Results.NotFound() : Results.Ok(log);
        }).WithName("GetAuditLogDetail").WithOpenApi();

        // GET /api/audit-logs/entity/{entityId} — all changes for a specific entity
        group.MapGet("/entity/{entityId:guid}", async (
            Guid entityId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.AuditLogs
                .Where(a => a.EntityId == entityId)
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id, a.UserId, a.UserName, a.Action,
                    a.EntityType, a.OldValues, a.NewValues, a.CreatedAt
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        }).WithName("GetEntityAuditTrail").WithOpenApi();

        // GET /api/audit-logs/summary — summary of recent audit activity
        group.MapGet("/summary", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();

            var last24h = DateTime.UtcNow.AddHours(-24);
            var recentCount = await db.AuditLogs.CountAsync(a => a.PropertyId == propertyId && a.CreatedAt >= last24h);

            var byAction = await db.AuditLogs
                .Where(a => a.PropertyId == propertyId && a.CreatedAt >= last24h)
                .GroupBy(a => a.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToListAsync();

            var byEntity = await db.AuditLogs
                .Where(a => a.PropertyId == propertyId && a.CreatedAt >= last24h)
                .GroupBy(a => a.EntityType)
                .Select(g => new { EntityType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10).ToListAsync();

            return Results.Ok(new
            {
                TotalChanges24h = recentCount,
                ByAction = byAction,
                ByEntity = byEntity
            });
        }).WithName("GetAuditSummary").WithOpenApi();
    }
}
