using System;

namespace SAFARIstack.Core.Domain.Security;

/// <summary>
/// Severity level of a security alert
/// </summary>
public enum SecurityAlertSeverity
{
    /// <summary>Informational event - no action needed</summary>
    Info = 0,
    
    /// <summary>Warning - may need attention</summary>
    Warning = 1,
    
    /// <summary>Critical security issue - requires immediate action</summary>
    Critical = 2,
    
    /// <summary>Severe breach - invoke emergency protocols</summary>
    Severe = 3
}

/// <summary>
/// Type of security alert
/// </summary>
public enum SecurityAlertType
{
    /// <summary>Failed authentication attempt</summary>
    FailedAuthentication,
    
    /// <summary>Unusual access pattern detected</summary>
    UnusualAccessPattern,
    
    /// <summary>Potential data exfiltration</summary>
    DataExfiltration,
    
    /// <summary>Database anomaly detected</summary>
    DatabaseAnomaly,
    
    /// <summary>After-hours admin access</summary>
    AfterHoursAccess,
    
    /// <summary>UnAuthorized API access attempt</summary>
    UnauthorizedApiAccess,
    
    /// <summary>Brute force attack detected</summary>
    BruteForceAttack,
    
    /// <summary>Privilege escalation attempt</summary>
    PrivilegeEscalation,
    
    /// <summary>Configuration tampering detected</summary>
    ConfigurationTampering,
    
    /// <summary>Rate limiting threshold exceeded</summary>
    RateLimitExceeded,
    
    /// <summary>Suspicious file operation</summary>
    SuspiciousFileOperation,
    
    /// <summary>Network intrusion attempt</summary>
    NetworkIntrusion,
    
    /// <summary>Compliance violation</summary>
    ComplianceViolation,
    
    /// <summary>Other security event</summary>
    Other
}

/// <summary>
/// Status of a security alert
/// </summary>
public enum SecurityAlertStatus
{
    /// <summary>Alert is new and not yet reviewed</summary>
    New,
    
    /// <summary>Alert has been acknowledged by admin</summary>
    Acknowledged,
    
    /// <summary>Alert is being investigated</summary>
    Investigating,
    
    /// <summary>Alert has been resolved</summary>
    Resolved,
    
    /// <summary>Alert was false positive</summary>
    FalsePositive,
    
    /// <summary>Alert was acknowledged as safe</summary>
    SafeEvent
}

/// <summary>
/// Autonomous response action taken by NullClaw
/// </summary>
public enum AutonomousResponseAction
{
    /// <summary>No autonomous action taken - manual review required</summary>
    ManualOnly,
    
    /// <summary>Alert was logged and monitored</summary>
    Logged,
    
    /// <summary>User account was rate-limited temporarily</summary>
    RateLimited,
    
    /// <summary>Session was terminated</summary>
    SessionTerminated,
    
    /// <summary>Access was temporarily blocked</summary>
    AccessBlocked,
    
    /// <summary>Additional authentication required</summary>
    MFARequired,
    
    /// <summary>Admin notification was sent</summary>
    AdminNotified,
    
    /// <summary>Automated incident response triggered</summary>
    IncidentResponseTriggered,
    
    /// <summary>System was put into restricted mode</summary>
    RestrictedModeEnabled
}

/// <summary>
/// Domain entity representing a security alert
/// </summary>
public class SecurityAlert
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Alert type/category
    /// </summary>
    public SecurityAlertType AlertType { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public SecurityAlertSeverity Severity { get; set; }

    /// <summary>
    /// Human-readable title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the alert
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Where the alert originated from (username, IP, service, etc.)
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Affected resource/entity ID
    /// </summary>
    public string? AffectedResourceId { get; set; }

    /// <summary>
    /// Affected resource type (e.g., "User", "Booking", "Guest")
    /// </summary>
    public string AffectedResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the alert
    /// </summary>
    public SecurityAlertStatus Status { get; set; } = SecurityAlertStatus.New;

    /// <summary>
    /// Autonomous response action NullClaw took
    /// </summary>
    public AutonomousResponseAction AutomousAction { get; set; } = AutonomousResponseAction.ManualOnly;

    /// <summary>
    /// Details of the autonomous action taken
    /// </summary>
    public string? AutomousActionDetails { get; set; }

    /// <summary>
    /// IP address associated with the alert
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User ID associated with the alert (if applicable)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Confidence score (0-100) that this is a real threat
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// JSON data with additional context
    /// </summary>
    public string ContextJson { get; set; } = "{}";

    /// <summary>
    /// Administrator notes
    /// </summary>
    public string? AdminNotes { get; set; }

    /// <summary>
    /// ID of admin who acknowledged the alert
    /// </summary>
    public string? AcknowledgedByAdminId { get; set; }

    /// <summary>
    /// When the alert was acknowledged
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// When the alert was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// When the alert was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the alert was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Flag indicating if this alert has been escalated
    /// </summary>
    public bool IsEscalated { get; set; }

    /// <summary>
    /// When the alert was escalated (if at all)
    /// </summary>
    public DateTime? EscalatedAt { get; set; }

    /// <summary>
    /// Flag indicating this is a production issue
    /// </summary>
    public bool IsProdIssue { get; set; }
}

/// <summary>
/// Represents an aggregate of security metrics
/// </summary>
public class SecurityMetrics
{
    /// <summary>
    /// Total alerts in last 24 hours
    /// </summary>
    public int TotalAlertsLast24Hours { get; set; }

    /// <summary>
    /// Critical alerts in last 24 hours
    /// </summary>
    public int CriticalAlertsLast24Hours { get; set; }

    /// <summary>
    /// Average time to resolve (seconds)
    /// </summary>
    public int AverageResolutionTime { get; set; }

    /// <summary>
    /// Percentage of alerts from unique sources
    /// </summary>
    public Dictionary<string, int> AlertsByType { get; set; } = new();

    /// <summary>
    /// Top affected resources
    /// </summary>
    public List<string> TopAffectedResources { get; set; } = new();

    /// <summary>
    /// Overall security score (0-100)
    /// </summary>
    public int SecurityScore { get; set; }

    /// <summary>
    /// When these metrics were computed
    /// </summary>
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
