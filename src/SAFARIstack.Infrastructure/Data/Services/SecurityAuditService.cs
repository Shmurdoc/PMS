using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Application.Services;
using SAFARIstack.Core.Domain.Security;

namespace SAFARIstack.Infrastructure.Data.Services;

/// <summary>
/// Implementation of security audit service using EF Core and PostgreSQL
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly Dictionary<string, DateTime> _blockedSources = new();

    public SecurityAuditService(ApplicationDbContext dbContext, ILogger<SecurityAuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SecurityAlert> LogSecurityEventAsync(
        SecurityAlertType alertType,
        SecurityAlertSeverity severity,
        string title,
        string description,
        string? sourceIp = null,
        string? userId = null,
        string? affectedResourceId = null,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        var alert = new SecurityAlert
        {
            Id = Guid.NewGuid(),
            AlertType = alertType,
            Severity = severity,
            Title = title,
            Description = description,
            Source = sourceIp ?? "unknown",
            UserId = userId,
            AffectedResourceId = affectedResourceId,
            ContextJson = context != null ? JsonSerializer.Serialize(context) : "{}",
            IpAddress = sourceIp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ConfidenceScore = CalculateConfidenceScore(alertType, severity)
        };

        _dbContext.SecurityAlerts.Add(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Security alert created: Type={AlertType}, Severity={Severity}, Title={Title}, Source={Source}",
            alertType, severity, title, sourceIp);

        // Trigger autonomous response if critical
        if (severity >= SecurityAlertSeverity.Critical)
        {
            await TriggerAutonomousResponseAsync(alert, cancellationToken);
        }

        return alert;
    }

    public async Task<List<SecurityAlert>> GetAlertsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        SecurityAlertSeverity? minSeverity = null,
        SecurityAlertStatus? status = null,
        int pageSize = 50,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SecurityAlerts.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate);

        if (minSeverity.HasValue)
            query = query.Where(a => a.Severity >= minSeverity);

        if (status.HasValue)
            query = query.Where(a => a.Status == status);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<SecurityAlert?> GetAlertAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SecurityAlerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
    }

    public async Task<bool> AcknowledgeAlertAsync(
        Guid alertId,
        string adminId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(alertId, cancellationToken);
        if (alert == null) return false;

        alert.Status = SecurityAlertStatus.Acknowledged;
        alert.AcknowledgedByAdminId = adminId;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AdminNotes = notes ?? alert.AdminNotes;
        alert.UpdatedAt = DateTime.UtcNow;

        _dbContext.SecurityAlerts.Update(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Security alert acknowledged: AlertId={AlertId}, AdminId={AdminId}", alertId, adminId);
        return true;
    }

    public async Task<bool> ResolveAlertAsync(
        Guid alertId,
        string adminId,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(alertId, cancellationToken);
        if (alert == null) return false;

        alert.Status = SecurityAlertStatus.Resolved;
        alert.ResolvedAt = DateTime.UtcNow;
        alert.AcknowledgedByAdminId = adminId;
        alert.AdminNotes = resolutionNotes ?? alert.AdminNotes;
        alert.UpdatedAt = DateTime.UtcNow;

        _dbContext.SecurityAlerts.Update(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Security alert resolved: AlertId={AlertId}, AdminId={AdminId}", alertId, adminId);
        return true;
    }

    public async Task<bool> EscalateAlertAsync(
        Guid alertId,
        string adminId,
        string? escalationReason = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(alertId, cancellationToken);
        if (alert == null) return false;

        alert.IsEscalated = true;
        alert.EscalatedAt = DateTime.UtcNow;
        alert.AdminNotes = escalationReason ?? alert.AdminNotes;
        alert.UpdatedAt = DateTime.UtcNow;

        _dbContext.SecurityAlerts.Update(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Security alert escalated: AlertId={AlertId}, Reason={Reason}", alertId, escalationReason);
        return true;
    }

    public async Task<bool> MarkAsFalsePositiveAsync(
        Guid alertId,
        string adminId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var alert = await GetAlertAsync(alertId, cancellationToken);
        if (alert == null) return false;

        alert.Status = SecurityAlertStatus.FalsePositive;
        alert.ResolvedAt = DateTime.UtcNow;
        alert.AcknowledgedByAdminId = adminId;
        alert.AdminNotes = reason ?? alert.AdminNotes;
        alert.UpdatedAt = DateTime.UtcNow;

        _dbContext.SecurityAlerts.Update(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alert marked as false positive: AlertId={AlertId}", alertId);
        return true;
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        fromDate ??= DateTime.UtcNow.AddDays(-1);
        toDate ??= DateTime.UtcNow;

        var alerts = await _dbContext.SecurityAlerts
            .Where(a => a.CreatedAt >= fromDate && a.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        var metrics = new SecurityMetrics
        {
            TotalAlertsLast24Hours = alerts.Count,
            CriticalAlertsLast24Hours = alerts.Count(a => a.Severity >= SecurityAlertSeverity.Critical),
            AlertsByType = alerts
                .GroupBy(a => a.AlertType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            TopAffectedResources = alerts
                .Where(a => !string.IsNullOrEmpty(a.AffectedResourceId))
                .GroupBy(a => a.AffectedResourceId)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key!)
                .ToList(),
            ComputedAt = DateTime.UtcNow
        };

        // Calculate average resolution time
        var resolvedAlerts = alerts.Where(a => a.ResolvedAt.HasValue).ToList();
        if (resolvedAlerts.Count > 0)
        {
            metrics.AverageResolutionTime = (int)resolvedAlerts
                .Average(a => (a.ResolvedAt!.Value - a.CreatedAt).TotalSeconds);
        }

        // Calculate security score (0-100)
        metrics.SecurityScore = CalculateSecurityScore(alerts);

        return metrics;
    }

    public async Task<bool> IsSourceBlocked(string sourceIdentifier, CancellationToken cancellationToken = default)
    {
        if (!_blockedSources.ContainsKey(sourceIdentifier))
            return false;

        var unblockTime = _blockedSources[sourceIdentifier];
        if (DateTime.UtcNow >= unblockTime)
        {
            _blockedSources.Remove(sourceIdentifier);
            return false;
        }

        return true;
    }

    public async Task<bool> BlockSourceAsync(
        string sourceIdentifier,
        TimeSpan duration,
        string reason = "",
        CancellationToken cancellationToken = default)
    {
        var unblockTime = DateTime.UtcNow.Add(duration);
        _blockedSources[sourceIdentifier] = unblockTime;

        _logger.LogWarning(
            "Source blocked: {Source}, Duration: {Duration}, Reason: {Reason}",
            sourceIdentifier, duration, reason);

        return true;
    }

    public async Task<bool> UnblockSourceAsync(string sourceIdentifier, CancellationToken cancellationToken = default)
    {
        if (_blockedSources.ContainsKey(sourceIdentifier))
        {
            _blockedSources.Remove(sourceIdentifier);
            _logger.LogInformation("Source unblocked: {Source}", sourceIdentifier);
            return true;
        }

        return false;
    }

    public async Task<AutonomousResponseAction> TriggerAutonomousResponseAsync(
        SecurityAlert alert,
        CancellationToken cancellationToken = default)
    {
        var action = AutonomousResponseAction.ManualOnly;
        var details = "";

        // Brute force attack - rate limit the source
        if (alert.AlertType == SecurityAlertType.BruteForceAttack)
        {
            action = AutonomousResponseAction.RateLimited;
            details = "Applied 5-minute rate limit to source";
            await BlockSourceAsync(alert.IpAddress ?? "unknown", TimeSpan.FromMinutes(5), "Brute force detected");
        }

        // After-hours access - require MFA
        if (alert.AlertType == SecurityAlertType.AfterHoursAccess && alert.Severity >= SecurityAlertSeverity.Critical)
        {
            action = AutonomousResponseAction.MFARequired;
            details = "Required additional MFA for user";
        }

        // Privilege escalation - block immediately
        if (alert.AlertType == SecurityAlertType.PrivilegeEscalation)
        {
            action = AutonomousResponseAction.SessionTerminated;
            details = "Terminated user session due to privilege escalation attempt";
        }

        alert.AutomousAction = action;
        alert.AutomousActionDetails = details;
        alert.UpdatedAt = DateTime.UtcNow;

        _dbContext.SecurityAlerts.Update(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Autonomous response triggered: AlertId={AlertId}, Action={Action}, Details={Details}",
            alert.Id, action, details);

        return action;
    }

    private int CalculateConfidenceScore(SecurityAlertType alertType, SecurityAlertSeverity severity)
    {
        // Base score on alert type
        var baseScore = alertType switch
        {
            SecurityAlertType.BruteForceAttack => 95,
            SecurityAlertType.PrivilegeEscalation => 90,
            SecurityAlertType.DataExfiltration => 85,
            SecurityAlertType.UnauthorizedApiAccess => 80,
            SecurityAlertType.FailedAuthentication => 40,
            SecurityAlertType.ConfigurationTampering => 88,
            _ => 50
        };

        // Adjust by severity
        var severityBonus = severity switch
        {
            SecurityAlertSeverity.Critical => 10,
            SecurityAlertSeverity.Severe => 15,
            SecurityAlertSeverity.Warning => -10,
            _ => 0
        };

        return Math.Clamp(baseScore + severityBonus, 0, 100);
    }

    private int CalculateSecurityScore(List<SecurityAlert> alerts)
    {
        if (alerts.Count == 0) return 100;

        var criticalCount = alerts.Count(a => a.Severity >= SecurityAlertSeverity.Critical);
        var pendingCount = alerts.Count(a => a.Status == SecurityAlertStatus.New || a.Status == SecurityAlertStatus.Investigating);

        var score = 100;
        score -= criticalCount * 5;
        score -= pendingCount * 2;

        return Math.Clamp(score, 0, 100);
    }
}
