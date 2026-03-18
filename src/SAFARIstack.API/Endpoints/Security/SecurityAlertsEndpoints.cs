using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SAFARIstack.Core.Application.Services;
using SAFARIstack.Core.Domain.Security;

namespace SAFARIstack.API.Endpoints.Security;

/// <summary>
/// API endpoints for security alerts (NullClaw integration)
/// </summary>
public static class SecurityAlertsEndpoints
{
    /// <summary>
    /// Map all security alert endpoints to the routing table
    /// </summary>
    public static void MapSecurityAlertsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/security/alerts")
            .WithName("SecurityAlerts")
            .WithOpenApi()
            .RequireAuthorization("AdminRequired");

        group.MapPost("/", CreateSecurityAlert)
            .WithName("CreateSecurityAlert")
            .WithSummary("Create a new security alert")
            .WithDescription("Reports a new security event that may trigger an alert");

        group.MapGet("/", GetSecurityAlerts)
            .WithName("GetSecurityAlerts")
            .WithSummary("Get recent security alerts")
            .WithDescription("Retrieves paginated list of security alerts with optional filtering");

        group.MapGet("/{alertId}", GetSecurityAlertById)
            .WithName("GetSecurityAlertById")
            .WithSummary("Get security alert details")
            .WithDescription("Retrieves details of a specific security alert");

        group.MapPost("/{alertId}/acknowledge", AcknowledgeAlert)
            .WithName("AcknowledgeAlert")
            .WithSummary("Acknowledge a security alert")
            .WithDescription("Mark an alert as acknowledged by an administrator");

        group.MapPost("/{alertId}/resolve", ResolveAlert)
            .WithName("ResolveAlert")
            .WithSummary("Resolve a security alert")
            .WithDescription("Mark an alert as resolved");

        group.MapPost("/{alertId}/escalate", EscalateAlert)
            .WithName("EscalateAlert")
            .WithSummary("Escalate a security alert")
            .WithDescription("Escalate an alert for urgent attention");

        group.MapPost("/{alertId}/false-positive", MarkAsFalsePositive)
            .WithName("MarkAsFalsePositive")
            .WithSummary("Mark alert as false positive")
            .WithDescription("Mark an alert as a false positive");

        group.MapGet("/metrics/dashboard", GetSecurityMetrics)
            .WithName("GetSecurityMetrics")
            .WithSummary("Get security dashboard metrics")
            .WithDescription("Get security overview metrics and statistics");

        group.MapGet("/blocked-sources/check", CheckBlockedSource)
            .WithName("CheckBlockedSource")
            .WithSummary("Check if source is blocked")
            .WithDescription("Check if a user/IP is currently blocked");

        group.MapPost("/blocked-sources/block", BlockSource)
            .WithName("BlockSource")
            .WithSummary("Block a source")
            .WithDescription("Manually block a source (user/IP)");

        group.MapPost("/blocked-sources/{sourceId}/unblock", UnblockSource)
            .WithName("UnblockSource")
            .WithSummary("Unblock a source")
            .WithDescription("Manually unblock a previously blocked source");
    }

    /// <summary>
    /// POST /api/v1/security/alerts
    /// Create a new security alert
    /// </summary>
    private static async Task<IResult> CreateSecurityAlert(
        CreateSecurityAlertRequest request,
        ISecurityAuditService securityService,
        CancellationToken cancellationToken = default)
    {
        var alert = await securityService.LogSecurityEventAsync(
            (SecurityAlertType)request.AlertType,
            (SecurityAlertSeverity)request.Severity,
            request.Title,
            request.Description,
            request.SourceIp,
            request.UserId?.ToString(),
            request.AffectedResourceId?.ToString(),
            request.Context,
            cancellationToken);

        return Results.Created($"/api/v1/security/alerts/{alert.Id}", new
        {
            alert.Id,
            alert.AlertType,
            alert.Severity,
            alert.Title,
            alert.Status,
            alert.AutomousAction,
            alert.CreatedAt
        });
    }

    /// <summary>
    /// GET /api/v1/security/alerts
    /// Get paginated list of security alerts
    /// </summary>
    private static async Task<IResult> GetSecurityAlerts(
        ISecurityAuditService securityService,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? minSeverity = null,
        [FromQuery] int? status = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var alerts = await securityService.GetAlertsAsync(
            from,
            to,
            minSeverity.HasValue ? (SecurityAlertSeverity)minSeverity : null,
            status.HasValue ? (SecurityAlertStatus)status : null,
            pageSize,
            pageNumber,
            cancellationToken);

        return Results.Ok(new
        {
            Count = alerts.Count,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Alerts = alerts.ConvertAll(a => new
            {
                a.Id,
                a.AlertType,
                a.Severity,
                a.Title,
                a.Status,
                a.AutomousAction,
                a.IpAddress,
                a.UserId,
                a.ConfidenceScore,
                a.IsEscalated,
                a.CreatedAt
            })
        });
    }

    /// <summary>
    /// GET /api/v1/security/alerts/{alertId}
    /// Get detailed alert information
    /// </summary>
    private static async Task<IResult> GetSecurityAlertById(
        Guid alertId,
        ISecurityAuditService securityService,
        CancellationToken cancellationToken = default)
    {
        var alert = await securityService.GetAlertAsync(alertId, cancellationToken);
        if (alert == null)
            return Results.NotFound(new { Error = "Alert not found" });

        return Results.Ok(new
        {
            alert.Id,
            alert.AlertType,
            alert.Severity,
            alert.Title,
            alert.Description,
            alert.Status,
            alert.Source,
            alert.IpAddress,
            alert.UserId,
            alert.AffectedResourceId,
            alert.AffectedResourceType,
            alert.AutomousAction,
            alert.AutomousActionDetails,
            alert.ConfidenceScore,
            alert.AdminNotes,
            alert.AcknowledgedAt,
            alert.ResolvedAt,
            alert.IsEscalated,
            alert.CreatedAt,
            alert.UpdatedAt
        });
    }

    /// <summary>
    /// POST /api/v1/security/alerts/{alertId}/acknowledge
    /// Acknowledge an alert
    /// </summary>
    private static async Task<IResult> AcknowledgeAlert(
        Guid alertId,
        AcknowledgeAlertRequest request,
        ISecurityAuditService securityService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var adminId = httpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Results.Unauthorized();

        var success = await securityService.AcknowledgeAlertAsync(alertId, adminId, request.Notes, cancellationToken);
        if (!success)
            return Results.NotFound(new { Error = "Alert not found" });

        return Results.Ok(new { Message = "Alert acknowledged successfully" });
    }

    /// <summary>
    /// POST /api/v1/security/alerts/{alertId}/resolve
    /// Resolve an alert
    /// </summary>
    private static async Task<IResult> ResolveAlert(
        Guid alertId,
        ResolveAlertRequest request,
        ISecurityAuditService securityService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var adminId = httpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Results.Unauthorized();

        var success = await securityService.ResolveAlertAsync(
            alertId, adminId, request.ResolutionNotes, cancellationToken);

        if (!success)
            return Results.NotFound(new { Error = "Alert not found" });

        return Results.Ok(new { Message = "Alert resolved successfully" });
    }

    /// <summary>
    /// POST /api/v1/security/alerts/{alertId}/escalate
    /// Escalate an alert
    /// </summary>
    private static async Task<IResult> EscalateAlert(
        Guid alertId,
        EscalateAlertRequest request,
        ISecurityAuditService securityService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var adminId = httpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Results.Unauthorized();

        var success = await securityService.EscalateAlertAsync(
            alertId, adminId, request.Reason, cancellationToken);

        if (!success)
            return Results.NotFound(new { Error = "Alert not found" });

        // TODO: Send notifications to on-call admin
        return Results.Ok(new { Message = "Alert escalated successfully" });
    }

    /// <summary>
    /// POST /api/v1/security/alerts/{alertId}/false-positive
    /// Mark an alert as false positive
    /// </summary>
    private static async Task<IResult> MarkAsFalsePositive(
        Guid alertId,
        FalsePositiveRequest request,
        ISecurityAuditService securityService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var adminId = httpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Results.Unauthorized();

        var success = await securityService.MarkAsFalsePositiveAsync(
            alertId, adminId, request.Reason, cancellationToken);

        if (!success)
            return Results.NotFound(new { Error = "Alert not found" });

        return Results.Ok(new { Message = "Alert marked as false positive" });
    }

    /// <summary>
    /// GET /api/v1/security/alerts/metrics/dashboard
    /// Get security metrics for dashboard
    /// </summary>
    private static async Task<IResult> GetSecurityMetrics(
        ISecurityAuditService securityService,
        CancellationToken cancellationToken = default)
    {
        var metrics = await securityService.GetSecurityMetricsAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            cancellationToken);

        return Results.Ok(metrics);
    }

    /// <summary>
    /// GET /api/v1/security/alerts/blocked-sources/check?source={source}
    /// Check if a source is blocked
    /// </summary>
    private static async Task<IResult> CheckBlockedSource(
        [FromQuery] string source,
        ISecurityAuditService securityService,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(source))
            return Results.BadRequest(new { Error = "Source parameter required" });

        var isBlocked = await securityService.IsSourceBlocked(source, cancellationToken);
        return Results.Ok(new { Source = source, IsBlocked = isBlocked });
    }

    /// <summary>
    /// POST /api/v1/security/alerts/blocked-sources/block
    /// Manually block a source
    /// </summary>
    private static async Task<IResult> BlockSource(
        BlockSourceRequest request,
        ISecurityAuditService securityService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var durationMinutes = request.DurationMinutes ?? 60;
        var success = await securityService.BlockSourceAsync(
            request.Source,
            TimeSpan.FromMinutes(durationMinutes),
            request.Reason ?? "",
            cancellationToken);

        if (!success)
            return Results.BadRequest(new { Error = "Failed to block source" });

        return Results.Ok(new
        {
            Message = $"Source {request.Source} blocked for {durationMinutes} minutes"
        });
    }

    /// <summary>
    /// POST /api/v1/security/alerts/blocked-sources/{sourceId}/unblock
    /// Unblock a source
    /// </summary>
    private static async Task<IResult> UnblockSource(
        string sourceId,
        ISecurityAuditService securityService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var success = await securityService.UnblockSourceAsync(sourceId, cancellationToken);
        if (!success)
            return Results.NotFound(new { Error = "Source not found in blocklist" });

        return Results.Ok(new { Message = $"Source {sourceId} unblocked successfully" });
    }
}

// Request/Response DTOs
public record CreateSecurityAlertRequest(
    int AlertType,
    int Severity,
    string Title,
    string Description,
    string? SourceIp = null,
    Guid? UserId = null,
    Guid? AffectedResourceId = null,
    Dictionary<string, object>? Context = null);

public record AcknowledgeAlertRequest(string? Notes = null);

public record ResolveAlertRequest(string? ResolutionNotes = null);

public record EscalateAlertRequest(string? Reason = null);

public record FalsePositiveRequest(string? Reason = null);

public record BlockSourceRequest(string Source, int? DurationMinutes = null, string? Reason = null);
