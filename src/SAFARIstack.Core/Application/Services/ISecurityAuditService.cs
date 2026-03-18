using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SAFARIstack.Core.Domain.Security;
using SAFARIstack.Core.Domain.Addons;

namespace SAFARIstack.Core.Application.Services;

/// <summary>
/// Service for managing security audits and alerts
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Log a security event that may trigger an alert
    /// </summary>
    Task<SecurityAlert> LogSecurityEventAsync(
        SecurityAlertType alertType,
        SecurityAlertSeverity severity,
        string title,
        string description,
        string? sourceIp = null,
        string? userId = null,
        string? affectedResourceId = null,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent alerts with optional filtering
    /// </summary>
    Task<List<SecurityAlert>> GetAlertsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        SecurityAlertSeverity? minSeverity = null,
        SecurityAlertStatus? status = null,
        int pageSize = 50,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alert by ID
    /// </summary>
    Task<SecurityAlert?> GetAlertAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(
        Guid alertId,
        string adminId,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve an alert
    /// </summary>
    Task<bool> ResolveAlertAsync(
        Guid alertId,
        string adminId,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Escalate an alert for urgent attention
    /// </summary>
    Task<bool> EscalateAlertAsync(
        Guid alertId,
        string adminId,
        string? escalationReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark alert as false positive
    /// </summary>
    Task<bool> MarkAsFalsePositiveAsync(
        Guid alertId,
        string adminId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security metrics/dashboard data
    /// </summary>
    Task<SecurityMetrics> GetSecurityMetricsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user/IP should be temporarily blocked due to suspicious activity
    /// </summary>
    Task<bool> IsSourceBlocked(string sourceIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Temporarily block a source (user/IP) to prevent brute force attacks
    /// </summary>
    Task<bool> BlockSourceAsync(
        string sourceIdentifier,
        TimeSpan duration,
        string reason = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblock a previously blocked source
    /// </summary>
    Task<bool> UnblockSourceAsync(string sourceIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trigger NullClaw's autonomous response system
    /// Returns the action NullClaw took
    /// </summary>
    Task<AutonomousResponseAction> TriggerAutonomousResponseAsync(
        SecurityAlert alert,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing addons
/// </summary>
public interface IAddOnManager
{
    /// <summary>
    /// Install a new addon
    /// </summary>
    Task<AddOnOperationResult> InstallAddOnAsync(
        string addOnId,
        string version,
        string packagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall an addon
    /// </summary>
    Task<AddOnOperationResult> UninstallAddOnAsync(
        string addOnId,
        bool force = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an addon to a new version
    /// </summary>
    Task<AddOnOperationResult> UpdateAddOnAsync(
        string addOnId,
        string newVersion,
        string packagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable/disable an addon without uninstalling
    /// </summary>
    Task<AddOnOperationResult> SetAddOnStateAsync(
        string addOnId,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all installed addons
    /// </summary>
    Task<List<AddOnLifecyclePhase>> GetInstalledAddOnsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get addon metadata
    /// </summary>
    Task<AddOnMetadata?> GetAddOnMetadataAsync(
        string addOnId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get addon configuration
    /// </summary>
    Task<Dictionary<string, object>> GetAddOnConfigAsync(
        string addOnId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update addon configuration
    /// </summary>
    Task<AddOnOperationResult> SetAddOnConfigAsync(
        string addOnId,
        Dictionary<string, object> config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check health of an addon
    /// </summary>
    Task<bool> IsAddOnHealthyAsync(string addOnId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trigger event listener for all subscribed addons
    /// </summary>
    Task<List<AddOnOperationResult>> RaiseEventAsync(
        AddOnEventHook eventHook,
        Dictionary<string, object> eventData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get addon-provided API routes
    /// </summary>
    Task<List<AddOnApiRoute>> GetAddOnRoutesAsync(string addOnId, CancellationToken cancellationToken = default);
}
