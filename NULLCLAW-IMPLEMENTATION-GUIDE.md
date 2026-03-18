# NullClaw Integration Implementation Guide

**Version**: 1.0  
**Date**: March 10, 2026  
**Status**: Production Implementation  
**Security Level**: Internal Use

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Installation & Setup](#installation--setup)
3. [Code Implementation](#code-implementation)
4. [Configuration](#configuration)
5. [Monitoring & Operations](#monitoring--operations)
6. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### How NullClaw Works with SAFARIstack

```
Real-Time Monitoring Layer
┌─────────────────────────────────────────────────────┐
│  NullClaw Agent (Autonomous AI Security)            │
│  • Monitors PostgreSQL activity logs                │
│  • Analyzes Redis cache access patterns             │
│  • Tracks API request anomalies                     │
│  • Detects guest data access patterns               │
│  • Identifies suspicious behavioral signatures      │
└─────────────────────────────────────────────────────┘
                     ↓
Alert Generation & Response
┌─────────────────────────────────────────────────────┐
│  Security Decision Engine                           │
│  • Low Risk: Log + Dashboard notification           │
│  • Medium Risk: Alert team + require verification   │
│  • High Risk: Block + notify security team          │
│  • Critical: Autonomous isolation + full lockdown   │
└─────────────────────────────────────────────────────┘
                     ↓
Response Execution
┌─────────────────────────────────────────────────────┐
│  Automated Actions                                  │
│  • Block IP at firewall level                       │
│  • Disable user account temporarily                 │
│  • Enable additional MFA requirement                │
│  • Activate logging on specific resources           │
│  • Trigger backup verification                      │
└─────────────────────────────────────────────────────┘
```

### Integration Points with SAFARIstack

| Component | Monitoring | Decision Point |
|-----------|-----------|-----------------|
| **PostgreSQL Auth** | auth_logs table | Detect brute force attempts |
| **API Layer** | Request logs | Detect unusual endpoint access |
| **Audit Trail** | audit_logs table | Detect unauthorized data access |
| **Guest Data** | Field access patterns | Detect POPIA violations |
| **Payment Processing** | PCI audit logs | Detect payment security issues |
| **Configuration Changes** | System events | Detect unauthorized changes |

---

## Installation & Setup

### Step 1: Prerequisites

```bash
# Install Docker (if not already installed)
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Verify Docker installation
docker --version

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### Step 2: Create NullClaw Workspace

```bash
# Create security workspace directory
sudo mkdir -p /opt/pms-security
cd /opt/pms-security

# Create subdirectories
sudo mkdir -p config logs workspace skills

# Set proper permissions
sudo chown -R 1000:1000 /opt/pms-security
chmod 700 /opt/pms-security
```

### Step 3: Create Configuration Files

Create `/opt/pms-security/config.json`:

```json
{
  "agent": {
    "name": "pms-security-guardian",
    "identity": "safaristack-security-v1",
    "version": "2.0.0"
  },
  "provider": {
    "type": "ollama",
    "model": "llama3.2",
    "base_url": "http://ollama:11434",
    "timeout_seconds": 30,
    "retry_attempts": 3
  },
  "memory": {
    "type": "sqlite",
    "path": "./workspace/security_memory.db",
    "vector_enabled": true,
    "retention_days": 90,
    "encryption_key": "${SECURITY_MEMORY_KEY}"
  },
  "channels": [
    {
      "type": "webhook",
      "name": "pms_alerts",
      "webhook_url": "${PMS_ALERT_WEBHOOK_URL}",
      "auth_token": "${WEBHOOK_SECRET}",
      "verify_ssl": true,
      "retry_on_failure": true,
      "retry_delay_ms": 5000,
      "max_retries": 3
    },
    {
      "type": "email",
      "name": "security_team",
      "smtp_host": "${SMTP_HOST}",
      "smtp_port": 587,
      "smtp_use_tls": true,
      "username": "${SMTP_USER}",
      "password": "${SMTP_PASSWORD}",
      "from_address": "security@yourdomain.com",
      "to_addresses": ["security@yourdomain.com", "ciso@yourdomain.com"],
      "alert_threshold": "High"
    },
    {
      "type": "slack",
      "name": "security_channel",
      "webhook_url": "${SLACK_WEBHOOK_URL}",
      "channel": "#security-alerts",
      "mention_on_critical": true
    }
  ],
  "tools": {
    "enabled": [
      "file_read",
      "file_write",
      "http_request",
      "shell",
      "memory_store",
      "memory_recall",
      "audit_log"
    ],
    "shell": {
      "allowed_commands": [
        "grep",
        "tail",
        "jq",
        "cat",
        "awk",
        "psql",
        "redis-cli",
        "curl",
        "date",
        "wc",
        "sort",
        "uniq"
      ],
      "blocked_commands": [
        "rm",
        "dd",
        "mkfs",
        "shutdown",
        "reboot"
      ],
      "timeout_seconds": 30,
      "max_output_size_mb": 100
    },
    "database": {
      "allowed_queries": [
        "SELECT.*FROM auth_logs",
        "SELECT.*FROM audit_logs",
        "SELECT.*FROM security_alerts"
      ],
      "blocked_patterns": ["DROP", "DELETE", "TRUNCATE", "UPDATE"]
    }
  },
  "autonomy": {
    "level": "medium",
    "max_actions_per_hour": 10,
    "require_approval_for_high_risk": true,
    "high_risk_actions": [
      "disable_user_account",
      "block_ip_address",
      "trigger_system_lockdown"
    ],
    "allowed_paths": [
      "/var/log/pms",
      "/var/log/postgresql",
      "/var/log/nginx",
      "/opt/pms-security/workspace"
    ]
  },
  "security": {
    "workspace_only": true,
    "sandbox": "auto",
    "encrypted_secrets": true,
    "audit_logging": true,
    "rate_limiting": {
      "enabled": true,
      "requests_per_minute": 60
    }
  },
  "monitoring": {
    "check_interval_seconds": 60,
    "alert_aggregation_seconds": 300,
    "metrics_enabled": true,
    "metrics_port": 9090
  },
  "compliance": {
    "popia_enabled": true,
    "pci_dss_enabled": true,
    "bbbee_enabled": false,
    "audit_retention_days": 2555
  }
}
```

### Step 4: Update Docker Compose

Add to existing `docker-compose.yml`:

```yaml
services:
  # ... existing services (postgres, redis, api, etc.)

  ollama:
    image: ollama/ollama:latest
    container_name: ollama-ai
    volumes:
      - ollama_data:/root/.ollama
    ports:
      - "11434:11434"
    environment:
      OLLAMA_NUM_PARALLEL: 1
      OLLAMA_NUM_THREAD: 4
      OLLAMA_MAX_LOADED_MODELS: 1
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:11434/api/tags"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - pms-network

  nullclaw-security:
    image: nullclaw/nullclaw:2.0.0
    container_name: nullclaw-pms-guardian
    volumes:
      - ./pms-security:/workspace
      - /var/log/pms:/var/log/pms:ro
      - /var/log/postgresql:/var/log/postgresql:ro
      - /var/log/nginx:/var/log/nginx:ro
      - nullclaw_data:/var/lib/nullclaw
    environment:
      NULLCLAW_CONFIG: /workspace/config.json
      NULLCLAW_LOGLEVEL: info
      WEBHOOK_SECRET: ${SECURITY_WEBHOOK_SECRET}
      PMS_ALERT_WEBHOOK_URL: ${PMS_ALERT_WEBHOOK_URL}
      SLACK_WEBHOOK_URL: ${SLACK_WEBHOOK_URL}
      SMTP_HOST: ${SMTP_HOST}
      SMTP_USER: ${SMTP_USER}
      SMTP_PASSWORD: ${SMTP_PASSWORD}
      SECURITY_MEMORY_KEY: ${SECURITY_MEMORY_KEY}
    ports:
      - "9090:9090"  # Metrics
    depends_on:
      - postgres
      - redis
      - ollama
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9090/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    restart: unless-stopped
    networks:
      - pms-network
    security_opt:
      - no-new-privileges:true

volumes:
  ollama_data:
  nullclaw_data:
```

### Step 5: Environment Variables

Create `.env.nullclaw`:

```bash
# NullClaw Configuration
SECURITY_WEBHOOK_SECRET=your-webhook-secret-key-here
PMS_ALERT_WEBHOOK_URL=https://pms.yourdomain.com/api/security/alerts
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/YOUR/WEBHOOK/URL
SMTP_HOST=smtp.gmail.com
SMTP_USER=security-alerts@yourdomain.com
SMTP_PASSWORD=your-app-specific-password
SECURITY_MEMORY_KEY=your-encryption-key-here

# Other services (existing)
DB_HOST=postgres
DB_USER=pms
DB_PASSWORD=secure-password-here
REDIS_HOST=redis
```

### Step 6: Start Services

```bash
# Load environment variables
source .env.nullclaw

# Start all services including NullClaw
docker-compose up -d

# Verify NullClaw is running
docker-compose logs -f nullclaw-security

# Wait for services to be healthy
docker-compose ps
```

---

## Code Implementation

### Part 1: Security Alert Endpoint

Create `src/SAFARIstack.API/Endpoints/SecurityAlertsEndpoint.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace SAFARIstack.API.Endpoints;

public static class SecurityAlertsEndpoint
{
    public static void MapSecurityAlertEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/security/alerts")
            .WithTags("Security")
            .WithName("SecurityAlerts");

        group.MapPost("/", ReportSecurityAlert)
            .WithName("ReportSecurityAlert")
            .AllowAnonymous()  // NullClaw signs requests
            .Produces<AlertReceivedResponse>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        group.MapGet("/history", GetAlertHistory)
            .WithName("GetSecurityAlertHistory")
            .RequireAuthorization("AdminOnly")
            .Produces<List<SecurityAlertDto>>(StatusCodes.Status200OK)
            .WithOpenApi();

        group.MapPost("/{id}/acknowledge", AcknowledgeAlert)
            .WithName("AcknowledgeAlert")
            .RequireAuthorization("AdminOnly")
            .Produces<AlertAcknowledgmentResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        group.MapGet("/{id}/details", GetAlertDetails)
            .WithName("GetAlertDetails")
            .RequireAuthorization("AdminOnly")
            .Produces<SecurityAlertDetailDto>(StatusCodes.Status200OK)
            .WithOpenApi();
    }

    // Handler
    private static async Task<IResult> ReportSecurityAlert(
        HttpContext context,
        [FromBody] SecurityAlertRequest request,
        ISecurityAuditService auditService,
        INotificationService notificationService,
        IMediator mediatr)
    {
        // Verify NullClaw signature
        var signature = context.Request.Headers["X-NullClaw-Signature"].ToString();
        if (!VerifyNullClawSignature(request, signature))
        {
            return Results.Unauthorized();
        }

        try
        {
            // Log the alert
            var alert = await auditService.LogSecurityAlert(
                request.AlertType,
                request.Severity,
                request.Description,
                request.Details);

            // Determine response action
            var responseAction = DetermineResponseAction(request.Severity);

            // Execute automated response if needed
            if (responseAction != ResponseAction.LogOnly)
            {
                var command = new ExecuteSecurityResponseCommand(
                    alert.Id,
                    responseAction,
                    request.Details);

                await mediatr.Send(command);
            }

            // Notify relevant stakeholders
            if (request.Severity is AlertSeverity.High or AlertSeverity.Critical)
            {
                await notificationService.NotifySecurityTeamAsync(alert);
            }

            return Results.Accepted("/api/v1/security/alerts/" + alert.Id,
                new AlertReceivedResponse
                {
                    AlertId = alert.Id,
                    Status = "Received and Processing",
                    Timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            // Log but don't expose details
            await auditService.LogSystemErrorAsync(
                "SecurityAlertProcessing",
                ex.Message);

            return Results.StatusCode(500);
        }
    }

    private static async Task<IResult> GetAlertHistory(
        ISecurityAuditService auditService,
        [FromQuery] int days = 7,
        [FromQuery] int pageSize = 50)
    {
        var alerts = await auditService.GetSecurityAlertsAsync(
            DateTime.UtcNow.AddDays(-days),
            pageSize);

        var dtos = alerts.Select(a => new SecurityAlertDto
        {
            Id = a.Id,
            AlertType = a.AlertType,
            Severity = a.Severity.ToString(),
            Description = a.Description,
            DetectedAt = a.DetectedAt,
            RespondedAt = a.RespondedAt,
            Status = a.Status
        }).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> AcknowledgeAlert(
        Guid id,
        ISecurityAuditService auditService,
        HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) 
            return Results.Unauthorized();

        var acknowledged = await auditService.AcknowledgeAlertAsync(
            id,
            Guid.Parse(userId));

        if (!acknowledged)
            return Results.NotFound();

        return Results.Ok(new AlertAcknowledgmentResponse
        {
            AlertId = id,
            AcknowledgedAt = DateTime.UtcNow,
            AcknowledgedBy = userId
        });
    }

    private static async Task<IResult> GetAlertDetails(
        Guid id,
        ISecurityAuditService auditService)
    {
        var alert = await auditService.GetAlertDetailsAsync(id);
        if (alert == null)
            return Results.NotFound();

        return Results.Ok(new SecurityAlertDetailDto
        {
            Id = alert.Id,
            AlertType = alert.AlertType,
            Severity = alert.Severity.ToString(),
            Description = alert.Description,
            Details = alert.Details,
            DetectedAt = alert.DetectedAt,
            RespondedAt = alert.RespondedAt,
            ResponseAction = alert.ResponseAction,
            Status = alert.Status
        });
    }

    // Verification
    private static bool VerifyNullClawSignature(
        object request,
        string signature)
    {
        // Get webhook secret from configuration
        var secret = Environment.GetEnvironmentVariable("WEBHOOK_SECRET");
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature))
            return false;

        // Serialize and hash
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(json)));

        // Compare
        return computedHash.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    private static ResponseAction DetermineResponseAction(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Low => ResponseAction.LogOnly,
            AlertSeverity.Medium => ResponseAction.LogAndNotify,
            AlertSeverity.High => ResponseAction.LogNotifyAndBlock,
            AlertSeverity.Critical => ResponseAction.FullLockdown,
            _ => ResponseAction.LogOnly
        };
    }
}

// DTOs
public record SecurityAlertRequest(
    string AlertType,
    AlertSeverity Severity,
    string Description,
    Dictionary<string, object> Details,
    DateTime Timestamp);

public record AlertReceivedResponse(
    Guid AlertId,
    string Status,
    DateTime Timestamp);

public record SecurityAlertDto(
    Guid Id,
    string AlertType,
    string Severity,
    string Description,
    DateTime DetectedAt,
    DateTime? RespondedAt,
    string Status);

public record AlertAcknowledgmentResponse(
    Guid AlertId,
    DateTime AcknowledgedAt,
    string AcknowledgedBy);

public record SecurityAlertDetailDto(
    Guid Id,
    string AlertType,
    string Severity,
    string Description,
    Dictionary<string, object> Details,
    DateTime DetectedAt,
    DateTime? RespondedAt,
    string ResponseAction,
    string Status);

public enum AlertSeverity { Low, Medium, High, Critical }
public enum ResponseAction { LogOnly, LogAndNotify, LogNotifyAndBlock, FullLockdown }
```

### Part 2: Security Audit Service

Create `src/SAFARIstack.Infrastructure/Security/SecurityAuditService.cs`:

```csharp
using SAFARIstack.Core.Domain;

namespace SAFARIstack.Infrastructure.Security;

public interface ISecurityAuditService
{
    Task<SecurityAlert> LogSecurityAlert(
        string alertType,
        AlertSeverity severity,
        string description,
        Dictionary<string, object> details);

    Task<List<SecurityAlert>> GetSecurityAlertsAsync(
        DateTime from,
        int pageSize);

    Task<SecurityAlert?> GetAlertDetailsAsync(Guid id);

    Task<bool> AcknowledgeAlertAsync(Guid alertId, Guid acknowledgedBy);

    Task LogSystemErrorAsync(string component, string error);
}

public class SecurityAuditService : ISecurityAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(
        ApplicationDbContext context,
        ILogger<SecurityAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SecurityAlert> LogSecurityAlert(
        string alertType,
        AlertSeverity severity,
        string description,
        Dictionary<string, object> details)
    {
        var alert = new SecurityAlert
        {
            Id = Guid.NewGuid(),
            AlertType = alertType,
            Severity = severity,
            Description = description,
            Details = details,
            DetectedAt = DateTime.UtcNow,
            Status = "Open"
        };

        _context.SecurityAlerts.Add(alert);
        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "Security Alert Logged: {AlertType} - {Severity}",
            alertType,
            severity);

        return alert;
    }

    public async Task<List<SecurityAlert>> GetSecurityAlertsAsync(
        DateTime from,
        int pageSize)
    {
        return await _context.SecurityAlerts
            .Where(a => a.DetectedAt >= from)
            .OrderByDescending(a => a.DetectedAt)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<SecurityAlert?> GetAlertDetailsAsync(Guid id)
    {
        return await _context.SecurityAlerts
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid alertId, Guid acknowledgedBy)
    {
        var alert = await _context.SecurityAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId);

        if (alert == null)
            return false;

        alert.Status = "Acknowledged";
        alert.RespondedAt = DateTime.UtcNow;
        alert.RespondedBy = acknowledgedBy;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task LogSystemErrorAsync(string component, string error)
    {
        _logger.LogError(
            "System Error in {Component}: {Error}",
            component,
            error);

        // Could also store in database for compliance
    }
}
```

### Part 3: DI Registration

Update `Program.cs`:

```csharp
// Add to DI container
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();

// Map endpoints
app.MapSecurityAlertEndpoints();
```

---

## Configuration

### Environment Variables Template

Create `.env.security.example`:

```bash
# NullClaw Security Configuration

# Webhook Configuration
SECURITY_WEBHOOK_SECRET=generate-strong-random-key-here
PMS_ALERT_WEBHOOK_URL=https://your-pms-domain.com/api/v1/security/alerts

# Notifications
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/YOUR/WEBHOOK/URL
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASSWORD=your-app-password

# Encryption
SECURITY_MEMORY_KEY=generate-strong-random-32-char-key

# Database
NULLCLAW_DB_HOST=postgres
NULLCLAW_DB_USER=pms
NULLCLAW_DB_PASSWORD=secure-password

# Log Level
NULLCLAW_LOGLEVEL=info
```

### Kubernetes Secrets

```bash
# Create secret for NullClaw
kubectl create secret generic nullclaw-secrets \
  --from-literal=webhook-secret=$(openssl rand -hex 32) \
  --from-literal=memory-key=$(openssl rand -hex 16) \
  --from-literal=slack-webhook=https://hooks.slack.com/... \
  -n pms-prod

# Reference in deployment:
# env:
#   - name: WEBHOOK_SECRET
#     valueFrom:
#       secretKeyRef:
#         name: nullclaw-secrets
#         key: webhook-secret
```

---

## Monitoring & Operations

### Health Check Integration

```csharp
// Add to health checks
builder.Services.AddHealthChecks()
    .AddCheck<NullClawHealthCheck>("nullclaw", tags: new[] { "security" })
    .AddCheck<PostgresAuthLogsHealthCheck>("postgres-authlogs", tags: new[] { "security" });

public class NullClawHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public NullClawHealthCheck(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "http://nullclaw-security:9090/health",
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("NullClaw is operational")
                : HealthCheckResult.Unhealthy("NullClaw returned error");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("NullClaw is unreachable", ex);
        }
    }
}
```

### Prometheus Metrics

```csharp
// Security metrics
public static class SecurityMetrics
{
    private static readonly Counter SecurityAlertsCounter = Counter
        .Create("pms_security_alerts_total", "Total security alerts", "severity");

    private static readonly Gauge ActiveSecurityIncidents = Gauge
        .Create("pms_active_security_incidents", "Active security incidents");

    private static readonly Histogram SecurityResponseTime = Histogram
        .Create("pms_security_response_seconds", "Security response time");

    public static void RecordAlert(string severity) =>
        SecurityAlertsCounter.Labels(severity).Inc();

    public static void SetActiveIncidents(double count) =>
        ActiveSecurityIncidents.Set(count);

    public static IDisposable MeasureResponseTime() =>
        SecurityResponseTime.NewTimer();
}
```

---

## Troubleshooting

### NullClaw Not Connecting

```bash
# Check logs
docker-compose logs nullclaw-security

# Verify network connectivity
docker exec nullclaw-security ping postgres
docker exec nullclaw-security curl http://ollama:11434/api/tags

# Check configuration
docker exec nullclaw-security cat /workspace/config.json
```

### High False Positive Rate

```json
{
  "tuning": {
    "failed_login_threshold": 10,  // Increase from 5
    "unusual_access_sensitivity": 0.7,  // Decrease from 0.9
    "rate_limit_threshold": 1000  // Increase from 500
  }
}
```

### Performance Degradation

Check NullClaw resource usage:

```bash
docker stats nullclaw-security

# If CPU/Memory high:
# 1. Reduce check_interval_seconds in config.json
# 2. Reduce ollama model size
# 3. Enable sampling (only check 10% of events)
```

---

*Implementation Guide - March 10, 2026*  
*Part of SAFARIstack Enterprise Upgrade*
