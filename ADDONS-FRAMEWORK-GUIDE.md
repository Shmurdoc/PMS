# SAFARIstack Add-ons Framework & Developer Guide

**Version**: 1.0  
**Date**: March 10, 2026  
**Status**: Production Ready  
**Audience**: Add-on Developers, System Architects

---

## Table of Contents

1. [Framework Overview](#framework-overview)
2. [Add-on Architecture](#add-on-architecture)
3. [Getting Started](#getting-started)
4. [API Reference](#api-reference)
5. [Deployment & Testing](#deployment--testing)
6. [Official Add-ons](#official-add-ons)
7. [Best Practices](#best-practices)

---

## Framework Overview

### What is the Add-ons Framework?

The SAFARIstack Add-ons Framework is a **plugin architecture** that allows developers to extend SAFARIstack PMS without modifying core code. Add-ons can:

- Add new API endpoints
- Create new data tables
- Integrate with external services
- Provide additional UI components
- Implement custom business logic
- Access all PMS services via interfaces

### Why Add-ons?

✅ **Isolation** - Add-ons don't affect core system  
✅ **Extensibility** - Add features without forking  
✅ **Compatibility** - Work across versions  
✅ **Monetization** - Sell premium add-ons  
✅ **Community** - Open ecosystem for third-party developers  

---

## Add-on Architecture

### Structure

```
MyAddOn/
├── MyAddOn.csproj                 # Add-on project file
├── manifest.json                  # Add-on metadata
├── README.md                       # Add-on documentation
├── LICENSE                         # License file
├── src/
│   ├── MyAddOnModule.cs            # Main module class (IAddOn interface)
│   ├── Features/
│   │   ├── Feature1/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── Services/
│   │   └── Feature2/
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Events/
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   └── ExternalServices/
│   └── API/
│       └── Endpoints/
├── migrations/
│   └── 001_InitialSchema.sql
├── tests/
│   └── MyAddOn.Tests.csproj
└── docs/
    ├── INSTALLATION.md
    ├── CONFIGURATION.md
    └── API.md
```

### Core Interfaces

```csharp
// Main interface - all add-ons must implement this
public interface IAddOn
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    string Author { get; }
    string Description { get; }
    Version MinimumPMSVersion { get; }
    
    // Lifecycle
    Task<bool> CanInstallAsync(IServiceProvider services);
    Task InstallAsync(IServiceCollection services, IApplicationDbContext dbContext);
    Task UninstallAsync(IServiceProvider services, IApplicationDbContext dbContext);
    Task<bool> IsHealthyAsync(IServiceProvider services);
    
    // Hooks
    Task OnBeforeInitializeAsync();
    Task OnAfterInitializeAsync();
    Task OnApplicationStartedAsync();
}

// Optional: Notification hook for system events
public interface IAddOnEventListener
{
    Task OnBookingCreatedAsync(BookingCreatedEvent @event);
    Task OnGuestCheckedInAsync(GuestCheckedInEvent @event);
    Task OnPaymentProcessedAsync(PaymentProcessedEvent @event);
    // ... other system events
}

// Configuration
public interface IAddOnConfiguration
{
    Task<T> GetConfigAsync<T>(string key);
    Task SetConfigAsync<T>(string key, T value);
    Task DeleteConfigAsync(string key);
}

// Database access
public interface IAddOnDataAccess
{
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task<T?> FindByIdAsync<T>(object id) where T : class;
}
```

---

## Getting Started

### Step 1: Create Project Structure

```bash
# Clone template
git clone https://github.com/safaristack/addon-template MyAddOn
cd MyAddOn

# Or create from scratch
dotnet new classlib -n SAFARIstack.AddOns.MyAddOn -f net9.0
cd SAFARIstack.AddOns.MyAddOn

# Add NuGet dependencies
dotnet add package SAFARIstack.AddOns.Framework --version 1.0.0
dotnet add package MediatR --version 12.0.0
dotnet add package FluentValidation --version 11.0.0
```

### Step 2: Create Manifest File

Create `manifest.json`:

```json
{
  "id": "addon-revenue-management-pro",
  "name": "Revenue Management System Pro",
  "version": "1.0.0",
  "author": "SAFARIstack Team",
  "description": "Enterprise revenue management with dynamic pricing and demand forecasting",
  "minimumPmsVersion": "2.1.0",
  "maximumPmsVersion": "3.0.0",
  "dependencies": [
    "addon-analytics",
    "addon-channels"
  ],
  "providedServices": [
    "IRevenueManagementService",
    "IPricingRecommendationService"
  ],
  "endpoints": [
    "/api/v1/revenue",
    "/api/v1/pricing"
  ],
  "database": {
    "tables": [
      "revenue_recommendations",
      "pricing_history",
      "demand_signals"
    ],
    "migrations": [
      "migrations/001_InitialSchema.sql"
    ]
  },
  "settings": {
    "enabled": true,
    "enabledByDefault": true,
    "licenseRequired": true,
    "licenseType": "commercial",
    "pricingTier": "enterprise"
  },
  "documentation": {
    "installationUrl": "https://docs.safaristack.com/addons/revenue-management/install",
    "configurationUrl": "https://docs.safaristack.com/addons/revenue-management/config",
    "apiUrl": "https://docs.safaristack.com/addons/revenue-management/api"
  }
}
```

### Step 3: Implement Main Module

Create `MyAddOnModule.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SAFARIstack.AddOns.Framework;
using SAFARIstack.Core.Domain;

namespace SAFARIstack.AddOns.MyAddOn;

public class MyAddOnModule : IAddOn
{
    public string Id => "addon-my-addon";
    public string Name => "My Custom Add-on";
    public Version Version => new(1, 0, 0);
    public string Author => "Your Company";
    public string Description => "Extended functionality for SAFARIstack";
    public Version MinimumPMSVersion => new(2, 1, 0);

    public async Task<bool> CanInstallAsync(IServiceProvider services)
    {
        try
        {
            // Check prerequisites
            var dbContext = services.GetRequiredService<IApplicationDbContext>();
            
            // Verify database connectivity
            var canConnect = await dbContext.Database.CanConnectAsync();
            
            return canConnect;
        }
        catch
        {
            return false;
        }
    }

    public async Task InstallAsync(
        IServiceCollection services,
        IApplicationDbContext dbContext)
    {
        // Register services
        services.AddScoped<IMyAddOnService, MyAddOnService>();
        services.AddScoped<IMyAddOnEventListener, MyAddOnEventListener>();

        // Run migrations
        await RunMigrationsAsync(dbContext);

        // Seed initial data if needed
        await SeedDataAsync(dbContext);
    }

    public async Task UninstallAsync(
        IServiceProvider services,
        IApplicationDbContext dbContext)
    {
        // Cleanup: remove data, etc.
        // Only remove data if user explicitly chooses full uninstall
        // By default, keep user data for backup purposes
        
        var service = services.GetRequiredService<IMyAddOnService>();
        await service.CleanupAsync();
    }

    public async Task<bool> IsHealthyAsync(IServiceProvider services)
    {
        try
        {
            var service = services.GetRequiredService<IMyAddOnService>();
            return await service.HealthCheckAsync();
        }
        catch
        {
            return false;
        }
    }

    public Task OnBeforeInitializeAsync()
    {
        // Called before add-on initialization
        return Task.CompletedTask;
    }

    public Task OnAfterInitializeAsync()
    {
        // Called after add-on initialization
        return Task.CompletedTask;
    }

    public Task OnApplicationStartedAsync()
    {
        // Called when application starts
        return Task.CompletedTask;
    }

    // Helper methods
    private async Task RunMigrationsAsync(IApplicationDbContext dbContext)
    {
        // Execute migration SQL
        var migrationSql = await File.ReadAllTextAsync(
            "migrations/001_InitialSchema.sql");
        
        await dbContext.Database.ExecuteSqlAsync(
            new RawSqlString(migrationSql ?? string.Empty));
    }

    private async Task SeedDataAsync(IApplicationDbContext dbContext)
    {
        // Seed default data for add-on
        // Example: Create default settings, templates, etc.
        await dbContext.SaveChangesAsync();
    }
}
```

### Step 4: Implement Services

Create `Services/IMyAddOnService.cs`:

```csharp
namespace SAFARIstack.AddOns.MyAddOn.Services;

public interface IMyAddOnService
{
    Task<MyAddOnResult> ProcessAsync(MyAddOnRequest request);
    Task<bool> HealthCheckAsync();
    Task CleanupAsync();
}

public class MyAddOnService : IMyAddOnService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMediator _mediatr;
    private readonly ILogger<MyAddOnService> _logger;

    public MyAddOnService(
        IApplicationDbContext dbContext,
        IMediator mediatr,
        ILogger<MyAddOnService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mediatr = mediatr ?? throw new ArgumentNullException(nameof(mediatr));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MyAddOnResult> ProcessAsync(MyAddOnRequest request)
    {
        _logger.LogInformation("Processing request: {RequestId}", request.Id);
        
        // Your business logic here
        
        return new MyAddOnResult
        {
            Success = true,
            Message = "Operation completed successfully",
            Data = new { }
        };
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            return canConnect;
        }
        catch
        {
            return false;
        }
    }

    public async Task CleanupAsync()
    {
        // Cleanup logic
        _logger.LogInformation("Cleaning up MyAddOn resources");
        await Task.CompletedTask;
    }
}

public record MyAddOnRequest(Guid Id, string Data);
public record MyAddOnResult(bool Success, string Message, object Data);
```

### Step 5: Create API Endpoints

Create `API/Endpoints/MyAddOnEndpoints.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace SAFARIstack.AddOns.MyAddOn.API.Endpoints;

public static class MyAddOnEndpoints
{
    public static void MapMyAddOnEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/my-addon")
            .WithTags("My Add-on")
            .RequireAuthorization();

        group.MapGet("/", GetStatus)
            .WithName("GetMyAddOnStatus")
            .WithOpenApi();

        group.MapPost("/process", ProcessRequest)
            .WithName("ProcessCustomRequest")
            .Produces<MyAddOnResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        group.MapGet("/health", HealthCheck)
            .WithName("MyAddOnHealthCheck")
            .AllowAnonymous()
            .WithOpenApi();
    }

    private static async Task<IResult> GetStatus(
        IMyAddOnService service)
    {
        var isHealthy = await service.HealthCheckAsync();
        return Results.Ok(new { Status = isHealthy ? "Healthy" : "Unhealthy" });
    }

    private static async Task<IResult> ProcessRequest(
        MyAddOnRequest request,
        IMyAddOnService service)
    {
        var result = await service.ProcessAsync(request);
        return Results.Ok(new MyAddOnResponse(
            result.Success,
            result.Message,
            result.Data));
    }

    private static async Task<IResult> HealthCheck(
        IMyAddOnService service)
    {
        var isHealthy = await service.HealthCheckAsync();
        return Results.Ok(new { Healthy = isHealthy });
    }
}

public record MyAddOnResponse(bool Success, string Message, object Data);
```

---

## API Reference

### IAddOnManager Interface

```csharp
public interface IAddOnManager
{
    /// <summary>List all installed add-ons</summary>
    Task<List<AddOnInfo>> ListInstalledAddOnsAsync();

    /// <summary>Install an add-on from a package</summary>
    Task<InstallResult> InstallAddOnAsync(
        string addOnPath,
        string licenseKey = null);

    /// <summary>Uninstall an add-on</summary>
    Task<UninstallResult> UninstallAddOnAsync(string addOnId);

    /// <summary>Enable/disable an add-on</summary>
    Task<bool> SetAddOnEnabledAsync(string addOnId, bool enabled);

    /// <summary>Update an add-on to newer version</summary>
    Task<UpdateResult> UpdateAddOnAsync(string addOnId, string newVersion);

    /// <summary>Get add-on health status</summary>
    Task<AddOnHealthReport> GetHealthReportAsync(string addOnId);

    /// <summary>Get add-on configuration</summary>
    Task<AddOnConfig> GetConfigAsync(string addOnId);

    /// <summary>Update add-on configuration</summary>
    Task SetConfigAsync(string addOnId, AddOnConfig config);
}
```

### Add-on Manifest Schema

```json
{
  "$schema": "https://safaristack.com/schemas/addon-manifest.json",
  "id": "string (required, unique, lowercase-with-dashes)",
  "name": "string (required, display name)",
  "version": "string (required, semver)",
  "author": "string",
  "description": "string",
  "minimumPmsVersion": "string (semver)",
  "maximumPmsVersion": "string (semver)",
  "dependencies": ["array of addon ids"],
  "providedServices": ["array of service names"],
  "endpoints": ["array of endpoint paths"],
  "database": {
    "tables": ["array of table names"],
    "migrations": ["array of migration file paths"]
  },
  "settings": {
    "enabled": "boolean",
    "enabledByDefault": "boolean",
    "licenseRequired": "boolean",
    "licenseType": "commercial|free|trial",
    "pricingTier": "free|standard|professional|enterprise"
  },
  "documentation": {
    "installationUrl": "string",
    "configurationUrl": "string",
    "apiUrl": "string"
  }
}
```

---

## Deployment & Testing

### Unit Testing Template

Create `SAFARIstack.AddOns.MyAddOn.Tests.csproj`:

```csharp
using Xunit;
using Moq;

namespace SAFARIstack.AddOns.MyAddOn.Tests;

public class MyAddOnServiceTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IMediator> _mockMediatr;
    private readonly MyAddOnService _service;

    public MyAddOnServiceTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockMediatr = new Mock<IMediator>();
        _service = new MyAddOnService(
            _mockDbContext.Object,
            _mockMediatr.Object,
            new NullLogger<MyAddOnService>());
    }

    [Fact]
    public async Task ProcessAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new MyAddOnRequest(Guid.NewGuid(), "test");

        // Act
        var result = await _service.ProcessAsync(request);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task HealthCheckAsync_WithDatabaseConnection_ReturnsTrue()
    {
        // Arrange
        _mockDbContext
            .Setup(x => x.Database.CanConnectAsync(default))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HealthCheckAsync();

        // Assert
        Assert.True(result);
    }
}
```

### Integration Testing

```csharp
[Collection("Integration Tests")]
public class MyAddOnIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public MyAddOnIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
    }

    [Fact]
    public async Task ApiEndpoint_Returns200WithValidRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/my-addon/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    public Task InitializeAsync()
    {
        // Setup test data
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Cleanup
        return NullAsyncDisposable.Instance.DisposeAsync().AsTask();
    }
}
```

### Package & Distribute

```bash
# Create NuGet package
dotnet pack -c Release

# Or create ZIP package for air-gapped installation
mkdir -p dist
zip -r dist/addon-my-addon.zip \
  bin/Release/net9.0/ \
  manifest.json \
  migrations/ \
  README.md

# Install from file
# Copy to ~/.safaristack/addons/
# Or use web UI: Settings → Add-ons → Install from file
```

---

## Official Add-ons

### 1. Channel Manager Pro

```
ID: addon-channel-manager-pro
Features:
- Booking.com real-time sync
- Expedia Connect integration
- Airbnb API v2 support
- Agoda integration
- Rate parity monitoring
- Overbooking prevention
```

### 2. Revenue Management Pro

```
ID: addon-revenue-management-pro
Features:
- Dynamic pricing engine
- Demand forecasting
- Competitor rate monitoring
- Revenue optimization alerts
- AI-powered recommendations
```

### 3. Guest Experience Platform

```
ID: addon-guest-experience
Features:
- WhatsApp messaging
- SMS notifications
- Email automation
- In-app chat
- Activity booking
- Room service ordering
```

### 4. Business Intelligence

```
ID: addon-business-intelligence
Features:
- Interactive dashboards
- Custom report builder
- Power BI integration
- Real-time KPI monitoring
- Predictive analytics
```

### 5. Energy Management

```
ID: addon-energy-management
Features:
- IoT sensor integration
- Energy tracking
- Load shedding automation
- Cost analysis
- Sustainability reporting
```

---

## Best Practices

### 1. Error Handling

```csharp
public async Task<IResult> MyEndpoint(IMyAddOnService service)
{
    try
    {
        var result = await service.ProcessAsync(...);
        return Results.Ok(result);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new { ex.Errors });
    }
    catch (NotFoundException ex)
    {
        return Results.NotFound(new { ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        return Results.StatusCode(500);
    }
}
```

### 2. Logging

```csharp
_logger.LogInformation("Add-on operation started for property {PropertyId}", propertyId);
_logger.LogWarning("Degraded performance detected in {Feature}", feature);
_logger.LogError(exception, "Critical error in {Operation}", operation);
```

### 3. Database Transactions

```csharp
using (var transaction = await _dbContext.Database.BeginTransactionAsync())
{
    try
    {
        // Perform database operations
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 4. Async/Await

```csharp
// Always use async
public async Task<Result> DoSomethingAsync()
{
    var data = await repository.GetDataAsync();
    return await ProcessAsync(data);
}

// Avoid blocking calls
// ❌ BAD: Task.Result, Task.Wait()
// ✅ GOOD: await
```

### 5. Security

```csharp
// Always authorize critical endpoints
[Authorize(Policy = "AdminOnly")]
public async Task<IResult> DeleteData(Guid id)
{
    // Implementation
}

// Use parameterized queries
var result = await _dbContext.Database.ExecuteSqlAsync(
    new RawSqlString($"SELECT * FROM table WHERE id = {id}"));

// Validate input
[Route("api/v1/addon/data")]
public async Task<IResult> SetData(
    [Validate] SetDataRequest request)
{
    // Implementation
}
```

---

## Resources

- **Framework NuGet Package**: `SAFARIstack.AddOns.Framework`
- **Template Repository**: `https://github.com/safaristack/addon-template`
- **Documentation**: `https://docs.safaristack.com/addons`
- **Marketplace**: `https://marketplace.safaristack.com`
- **Community Forum**: `https://community.safaristack.com`
- **Support Email**: `addons-support@safaristack.com`

---

*Add-ons Framework Documentation*  
*Part of SAFARIstack Enterprise Upgrade*  
*March 10, 2026*
