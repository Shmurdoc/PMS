# Integration Guide: Adding Advanced Features to SAFARIstack

## Step-by-Step Integration

### Step 1: Update Program.cs with Module Registration

Add to `src/SAFARIstack.API/Program.cs` after existing service registrations:

```csharp
// ===== NEW: Advanced Feature Modules =====

// Analytics Module - Event-driven analytics layer
AnalyticsModule.RegisterServices(builder.Services);

// Events Module - Async messaging infrastructure
EventsModule.RegisterEventBus(builder.Services);

// Channels Module - OTA synchronization
ChannelsModule.RegisterServices(builder.Services);

// Revenue Module - RMS & pricing intelligence
RevenueModule.RegisterServices(builder.Services);

// Note: Additional modules (OnlineBooking, Operations, etc.) 
// will be registered as they are completed
```

### Step 2: Update Solution File

Add new projects to `backend/SAFARIstack.sln`:

```bash
# From backend directory
dotnet sln SAFARIstack.sln add src/SAFARIstack.Modules.Analytics/SAFARIstack.Modules.Analytics.csproj
dotnet sln SAFARIstack.sln add src/SAFARIstack.Modules.Events/SAFARIstack.Modules.Events.csproj
dotnet sln SAFARIstack.sln add src/SAFARIstack.Modules.Channels/SAFARIstack.Modules.Channels.csproj
dotnet sln SAFARIstack.sln add src/SAFARIstack.Modules.Revenue/SAFARIstack.Modules.Revenue.csproj
```

### Step 3: Create Event Handlers in Core Module

Update `src/SAFARIstack.Core/Application/Bookings/Commands/CreateBookingCommandHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Modules.Events;
using SAFARIstack.Modules.Events.Contracts;

namespace SAFARIstack.Core.Application.Bookings.Commands;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, CreateBookingResult>
{
    private readonly DbContext _context;
    private readonly IEventPublisher _eventPublisher; // NEW: Inject event publisher

    public CreateBookingCommandHandler(
        DbContext context,
        IEventPublisher eventPublisher) // NEW
    {
        _context = context;
        _eventPublisher = eventPublisher; // NEW
    }

    public async Task<CreateBookingResult> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken)
    {
        // ... existing booking creation logic ...

        // NEW: Publish event after successful creation
        var @event = new BookingConfirmedEvent(
            booking.Id,
            request.PropertyId,
            guestId, // or anonymized segment ID
            booking.TotalAmount,
            booking.Nights,
            booking.CheckInDate,
            booking.CheckOutDate,
            roomTypes: request.Rooms.Select(r => r.RoomTypeId.ToString()).ToArray()
        );

        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new CreateBookingResult(
            booking.Id,
            booking.BookingReference,
            booking.TotalAmount,
            true);
    }
}
```

### Step 4: Create Event Handlers for Analytics Module

Create `src/SAFARIstack.Modules.Analytics/Application/EventHandlers/BookingEventHandlers.cs`:

```csharp
using MassTransit;
using SAFARIstack.Modules.Analytics.Application.Services;
using SAFARIstack.Modules.Events.Contracts;

namespace SAFARIstack.Modules.Analytics.Application.EventHandlers;

public class BookingConfirmedEventHandler : IConsumer<BookingConfirmedEvent>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<BookingConfirmedEventHandler> _logger;

    public BookingConfirmedEventHandler(
        IAnalyticsService analyticsService,
        ILogger<BookingConfirmedEventHandler> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation(
            "Recording booking for analytics: {BookingId} at {PropertyId}",
            @event.BookingId,
            @event.PropertyId);

        // Record for occupancy forecasting
        var metadata = new Dictionary<string, object>
        {
            { "booking_id", @event.BookingId },
            { "nights", @event.NightCount },
            { "rate", @event.TotalRate },
            { "check_in", @event.CheckIn }
        };

        await _analyticsService.RecordGuestInteraction(
            @event.GuestSegmentId,
            "booking_confirmed",
            metadata,
            context.CancellationToken);
    }
}

public class CheckInCompletedEventHandler : IConsumer<CheckInCompletedEvent>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<CheckInCompletedEventHandler> _logger;

    public CheckInCompletedEventHandler(
        IAnalyticsService analyticsService,
        ILogger<CheckInCompletedEventHandler> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CheckInCompletedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation("Guest checked in: {BookingId}", @event.BookingId);

        var metadata = new Dictionary<string, object>
        {
            { "check_in_time", @event.ActualCheckInTime }
        };

        await _analyticsService.RecordGuestInteraction(
            @event.GuestSegmentId,
            "check_in_completed",
            metadata,
            context.CancellationToken);
    }
}
```

### Step 5: Create Event Handlers for Channels Module

Create `src/SAFARIstack.Modules.Channels/Application/EventHandlers/RateUpdateHandler.cs`:

```csharp
using MassTransit;
using SAFARIstack.Modules.Channels.Application.Services;
using SAFARIstack.Modules.Events.Contracts;

namespace SAFARIstack.Modules.Channels.Application.EventHandlers;

public class RateUpdatedEventHandler : IConsumer<RateUpdatedEvent>
{
    private readonly IChannelManager _channelManager;
    private readonly ILogger<RateUpdatedEventHandler> _logger;

    public RateUpdatedEventHandler(
        IChannelManager channelManager,
        ILogger<RateUpdatedEventHandler> logger)
    {
        _channelManager = channelManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RateUpdatedEvent> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation(
            "Syncing rate update to channels: {PropertyId} R{OldRate} -> R{NewRate}",
            @event.PropertyId,
            @event.OldRate,
            @event.NewRate);

        // TODO: Sync to each configured channel
        // var channels = GetConfiguredChannels(@event.PropertyId);
        // foreach (var channel in channels)
        // {
        //     await _channelManager.SyncRatesAsync(
        //         @event.PropertyId,
        //         channel.Name,
        //         new DeltaUpdate { ... });
        // }
    }
}
```

### Step 6: Create API Endpoint for Analytics

Create `src/SAFARIstack.API/Endpoints/AnalyticsEndpoints.cs`:

```csharp
using MediatR;
using SAFARIstack.Modules.Analytics.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics")
            .WithName("Analytics")
            .WithOpenApi();

        group.MapGet("/occupancy-forecast/{propertyId}", GetOccupancyForecast)
            .WithName("GetOccupancyForecast")
            .WithOpenApi();

        group.MapGet("/revenue-forecast/{propertyId}", GetRevenueForecast)
            .WithName("GetRevenueForecast")
            .WithOpenApi();

        group.MapGet("/report/{reportDefinitionId}", GenerateReport)
            .WithName("GenerateReport")
            .WithOpenApi();
    }

    private static async Task<IResult> GetOccupancyForecast(
        Guid propertyId,
        int daysAhead,
        IAnalyticsService analytics)
    {
        var forecast = await analytics.GetOccupancyForecast(propertyId, daysAhead);
        return Results.Ok(forecast);
    }

    private static async Task<IResult> GetRevenueForecast(
        Guid propertyId,
        DateTime startDate,
        DateTime endDate,
        IAnalyticsService analytics)
    {
        var period = new DateRange(startDate, endDate);
        var forecast = await analytics.GetRevenueForecast(propertyId, period);
        return Results.Ok(forecast);
    }

    private static async Task<IResult> GenerateReport(
        Guid reportDefinitionId,
        IAnalyticsService analytics)
    {
        // TODO: Look up report definition
        // var report = await analytics.GenerateReport(definition);
        return Results.Ok(new { message = "Report generation in progress" });
    }
}
```

Update `src/SAFARIstack.API/Program.cs` to map endpoints:

```csharp
// After health endpoint
app.MapAnalyticsEndpoints();
// Add more endpoint mappings as modules are completed
```

### Step 7: Update API Endpoints Registration in Program.cs

```csharp
// ===== API Endpoints Registration =====
app.MapBookingEndpoints();
app.MapStaffEndpoints();
app.MapRfidEndpoints();

// NEW: Advanced feature endpoints
app.MapAnalyticsEndpoints();
// app.MapRevenueEndpoints();
// app.MapChannelEndpoints();
```

---

## Building & Testing

### Build Everything
```bash
cd backend
dotnet clean
dotnet build
```

### Run Unit Tests
```bash
dotnet test --no-build
```

### Run the API
```bash
cd src/SAFARIstack.API
dotnet run
```

### Test Analytics Endpoint
```powershell
$propertyId = "550e8400-e29b-41d4-a716-446655440000"

# Get occupancy forecast
Invoke-RestMethod -Uri "http://localhost:5001/api/analytics/occupancy-forecast/$propertyId?daysAhead=30" -Method Get

# Get revenue forecast  
Invoke-RestMethod -Uri "http://localhost:5001/api/analytics/revenue-forecast/$propertyId?startDate=2026-02-10&endDate=2026-03-10" -Method Get
```

---

## Database Migrations

### Create Analytics Schema
```bash
cd backend

# Create migration for analytics tables
dotnet ef migrations add AddAnalyticsTables -p src/SAFARIstack.Infrastructure

# Apply migration
dotnet ef database update -p src/SAFARIstack.Infrastructure
```

### SQL for TimescaleDB (if using)
```sql
-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Create hypertable for occupancy forecast
CREATE TABLE IF NOT EXISTS occupancy_forecast (
    property_id UUID NOT NULL,
    forecast_date TIMESTAMP NOT NULL,
    days_ahead INT NOT NULL,
    predicted_occupancy_rate DECIMAL(5, 4),
    predicted_booked_rooms INT,
    total_available_rooms INT,
    confidence DECIMAL(5, 4),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

SELECT create_hypertable('occupancy_forecast', 'forecast_date', if_not_exists => TRUE);
CREATE INDEX idx_occupancy_forecast_property ON occupancy_forecast(property_id, forecast_date DESC);

-- Similar for revenue_forecast, guest_behavior_profile, etc.
```

---

## Configuration

### Add to appsettings.Development.json
```json
{
  "AnalyticsConfiguration": {
    "Enabled": true,
    "ForecastingDaysAhead": 90,
    "AggregationIntervalMinutes": 5,
    "AnalyticsDbConnectionString": "Host=localhost;Port=5432;Database=safaristack_analytics;Username=postgres;Password=postgres"
  },
  
  "ChannelsConfiguration": {
    "Enabled": true,
    "SyncIntervalMinutes": 5,
    "DeltaSyncEnabled": true,
    "ConflictResolutionStrategy": "automatic"
  },
  
  "RevenueConfiguration": {
    "Enabled": true,
    "RecommendationGenerationEnabled": true,
    "AlertingEnabled": true
  },
  
  "EventBusConfiguration": {
    "TransportType": "InMemory",
    "MaxRetries": 3,
    "RetryDelayMs": 1000
  }
}
```

### Add Feature Flags (in property configuration)
```json
{
  "propertyId": "550e8400-e29b-41d4-a716-446655440000",
  "enabledFeatures": {
    "analyticsAndReporting": true,
    "channelManager": true,
    "revenueManagement": true,
    "onlineBooking": false,
    "energyManagement": false,
    "complianceReporting": true
  }
}
```

---

## Monitoring & Observability

### Application Insights Integration
```csharp
builder.Services.AddApplicationInsightsTelemetry();

var telemetryClient = new TelemetryClient(new TelemetryConfiguration
{
    InstrumentationKey = builder.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]
});

// Log analytics events
telemetryClient.TrackEvent("OccupancyForecasted", new Dictionary<string, string>
{
    { "PropertyId", propertyId.ToString() },
    { "DaysAhead", "90" }
});
```

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("analytics-db", new DbContextHealthCheck<AnalyticsDbContext>())
    .AddCheck("event-bus", new EventBusHealthCheck())
    .AddCheck("cache", new RedisHealthCheck());
```

---

## Next Steps

1. **Build Solution**: Ensure all modules compile
2. **Run Tests**: Verify event handlers work correctly
3. **Deploy to Dev**: Test in non-production environment
4. **Monitor Events**: Track event flow through message bus
5. **Validate Analytics**: Confirm forecasts are being generated
6. **Implement Phase 2**: Start OnlineBooking and Operations modules

---

## Troubleshooting

### Events Not Being Published
- Check `IEventPublisher` is registered in DI
- Verify event handlers are consuming correct event types
- Look at MassTransit diagnostics

### Analytics Data Not Updating
- Verify analytics DB is accessible
- Check background aggregation job logs
- Ensure event handlers are being invoked

### High Memory Usage
- Reduce event bus queue size
- Enable event filtering (only subscribe to needed events)
- Implement event archival

---

## Questions & Support

For issues or questions regarding the advanced features architecture:

1. Review [ADVANCED-FEATURES-ARCHITECTURE.md](ADVANCED-FEATURES-ARCHITECTURE.md)
2. Check [ADVANCED-FEATURES-IMPLEMENTATION.md](ADVANCED-FEATURES-IMPLEMENTATION.md)
3. Review module contracts in `Domain/Interfaces/`
4. Check event contracts in `Contracts/DomainEventContracts.cs`

---

**Last Updated**: February 10, 2026  
**Architecture Status**: Phase 1 Complete, Ready for Integration
