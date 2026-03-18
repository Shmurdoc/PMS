# SAFARIstack Advanced Features Architecture Guide

## Overview

This document describes the architectural approach for implementing 20+ advanced features while maintaining **loose coupling**, **scalability**, and **simplicity**.

---

## Core Architectural Principles

### 1. **Event-Driven Architecture (Asynchronous Messaging)**

Instead of modules querying each other's databases directly, they communicate through **domain events**:

```
Core PMS
  ↓ (publishes)
BookingConfirmedEvent
  ↓ (consumed by)
┌─────────────────────┬──────────────────────┬─────────────────┐
Analytics Module      Channel Manager        Revenue Module
(records forecast)    (syncs to OTAs)        (analyzes demand)
```

**Benefits:**
- ✅ Zero direct coupling between modules
- ✅ Can add new subscribers without touching core
- ✅ Asynchronous = no blocking
- ✅ Can replay events for data recovery

### 2. **Modular Monolith (Independently Configurable)**

Each feature is a **self-contained module** that can be:
- ✅ Enabled/disabled per property
- ✅ Configured independently
- ✅ Upgraded separately
- ✅ Replaced with alternatives

```
Backend/src/
├── SAFARIstack.Modules.Analytics/       → Occupancy forecasting, Guest behavior
├── SAFARIstack.Modules.Channels/        → Booking.com, Expedia, Airbnb sync
├── SAFARIstack.Modules.Revenue/         → RMS, pricing recommendations
├── SAFARIstack.Modules.OnlineBooking/   → Standalone booking engine
├── SAFARIstack.Modules.Operations/      → POS, Maintenance, Inventory
├── SAFARIstack.Modules.Energy/          → IoT integration, load shedding
├── SAFARIstack.Modules.Compliance/      → SARS VAT, B-BBEE, POPIA
├── SAFARIstack.Modules.Safari/          → Wildlife, Vehicle Management
└── SAFARIstack.Modules.Events/          → Async messaging infrastructure
```

### 3. **CQRS Pattern (Separate Read & Write Models)**

- **Write Model**: Transactional database (PostgreSQL) - only updated by Core PMS
- **Read Model**: Analytics database (TimescaleDB) - updated via events
- **Cache Layer**: Redis - real-time metrics

```
Transactional DB              Analytics DB (Time-Series)     Cache (Redis)
(PostgreSQL)                  (TimescaleDB/ClickHouse)      
  │                                 ↑                          ↑
  ├─ Properties                     ├─ Occupancy_by_date       ├─ HourlyOccupancy
  ├─ Bookings      ──events──>      ├─ Revenue_by_date         ├─ TodayRevenue
  ├─ Guests                         ├─ Guest_behavior          ├─ ForecastOccupancy
  └─ Rooms                          └─ Event_log
```

### 4. **Contract-Based Integration (Interfaces)**

Modules depend on **interfaces** (contracts), not implementations:

```csharp
// Shared contract (in Shared project)
public interface IAnalyticsService
{
    Task<OccupancyForecast> GetOccupancyForecast(Guid propertyId, int daysAhead);
}

// Modules register in DI
services.AddScoped<IAnalyticsService, AnalyticsService>();

// Other modules consume via DI
public class RevenueService
{
    public RevenueService(IAnalyticsService analytics) { }
}
```

**Benefits:**
- ✅ Can swap implementations without changing consumers
- ✅ Easy to mock for testing
- ✅ Clear contract = clear expectations

---

## Module Architecture

### Analytics Module
**Purpose**: Event-driven, read-optimized analytics layer

**Not**: Direct queries on transactional DB
**Instead**: 
1. Subscribe to BookingConfirmedEvent, CheckInCompletedEvent, etc.
2. Write denormalized aggregates to TimescaleDB
3. Serve reports from analytics DB

```csharp
public class BookingConfirmedEventHandler : INotificationHandler<BookingConfirmedEvent>
{
    public async Task Handle(BookingConfirmedEvent evt, CancellationToken ct)
    {
        // Write to analytics DB, NOT transactional DB
        await _analyticsDb.RecordBookingForForecasting(evt);
    }
}
```

**Features**:
- 📊 Occupancy forecasting (ML integration ready)
- 📈 Revenue forecasting
- 👥 Guest behavior analysis (anonymized, POPIA compliant)
- 📋 Custom report builder (metadata-driven)
- 🔄 Background aggregation jobs

### Events Module
**Purpose**: Async messaging infrastructure

**Technology**: MassTransit (in-memory for dev, can switch to RabbitMQ/Azure Service Bus)

**Event Types**:
- Booking events (Confirmed, Modified, Cancelled, CheckIn/Out)
- Staff events (CheckIn, CheckOut)
- Rate events (Updated, Changed)
- Channel events (SyncRequested, SyncCompleted)
- Guest events (ServiceRequested, FeedbackReceived)
- Energy events (ConsumptionRecorded, AlertTriggered)
- Maintenance events (WorkOrderCreated, Completed)

### Channels Module (OTA Manager)
**Purpose**: 2-way sync with Booking.com, Expedia, Airbnb

**Architecture**:
```
Core PMS             Delta Update Engine        OTA APIs
Availability ──────> Check Overbooking ────────> Booking.com
Rates               Conflict Resolution        Expedia
Restrictions        Sync via Client Libs       Airbnb
                    ↓
              Event Published
              (OTASyncCompleted)
                    ↓
              Analytics & Revenue
              Module Subscribed
```

**Features**:
- ✅ Delta-based updates (only changed data)
- ✅ Conflict resolution (prevents overbooking)
- ✅ 2-way sync (pull rates/availability from OTAs)
- ✅ Periodic reconciliation (full sync fallback)

### Revenue Module (RMS)
**Purpose**: Pricing recommendations & market intelligence

**Important**: Decision-support, NOT enforcement

```
Demand Signals           Competitor Rates        Pricing Algorithm
(from events)    +       (from Channel Mgr)   ──> Recommendation
                 +       Seasonality                ↓
                 └──────────────────────────> Manager Reviews
                                             Accept/Reject
                                             ↓ (publishes RateUpdatedEvent)
                                             Channel Manager syncs
```

**Features**:
- 💡 Pricing recommendations (with confidence scores)
- 🛍️ Rate shopping intelligence
- ⚠️ Revenue alerts
- 📊 Demand signal aggregation

---

## Data Flow Example: Complete Booking → Analytics → Revenue → Channels Cycle

```
1. BOOKING CREATED (Core PMS)
   └─> publishes BookingConfirmedEvent
       {
         BookingId, PropertyId, GuestSegmentId,
         Rate, Nights, CheckIn, CheckOut, RoomTypes
       }

2. ANALYTICS MODULE RECEIVES
   └─> BookingConfirmedEventHandler
       └─> Writes to analytics DB:
           - Increment occupancy forecast
           - Update guest behavior profile
           - Record in booking trend
           - Publish to forecast aggregation

3. REVENUE MODULE RECEIVES
   └─> DemandSignalAggregator
       └─> Increment booking count for demand signal
           └─> Pricing algorithm checks if rate adjustment needed
               └─> If high demand detected:
                   └─> Publishes RateUpdatedEvent

4. CHANNEL MANAGER RECEIVES RateUpdatedEvent
   └─> Compares old vs new rate
       └─> Creates DeltaUpdate
           └─> Checks for overbooking
               └─> Syncs via OTA clients
                   └─> Publishes OTASyncCompletedEvent

5. ANALYTICS MODULE RECEIVES OTASyncCompletedEvent
   └─> Records channel sync success
       └─> Updates competitor rate data (if pulled from OTA)

RESULT: Single booking event triggers chain of intelligent responses
across 3 modules with ZERO coupling!
```

---

## Avoiding Over-Engineering

### ✅ What We Do

1. **Start Simple**: In-memory event bus, single analytics DB, basic pricing algorithm
2. **Interfaces First**: Define contracts, implement simply
3. **Async But Not Always**: Use async where it matters (I/O), not for CPU
4. **One Job Per Service**: AnalyticsService does analytics, not CRM sync

### ❌ What We Avoid

1. ❌ Distributed transaction saga (use event sourcing instead)
2. ❌ Complex event correlation (use IDs + timestamps)
3. ❌ Real-time microservices (monolith modules are fine)
4. ❌ Hardcoded schemas (use metadata-driven instead)

---

## Per-Property Configuration

Each property can enable/disable features:

```json
{
  "propertyId": "uuid-here",
  "enabledModules": {
    "analytics": true,
    "channelManager": true,
    "revenueManagement": true,
    "onlineBooking": false,
    "energyManagement": true
  },
  "channelConfiguration": {
    "bookingComEnabled": true,
    "expediaEnabled": true,
    "syncStrategy": "delta"
  },
  "analyticsConfiguration": {
    "reportingEnabled": true,
    "forecastingDaysAhead": 90,
    "customReports": ["Occupancy", "FinancialPerformance"]
  }
}
```

**Implementation**: Check enabled status before executing:

```csharp
public class AnalyticsService
{
    public async Task RecordInteraction(...)
    {
        if (!_propertyConfig.IsModuleEnabled("analytics"))
            return;
        
        await _analyticsDb.Record(...);
    }
}
```

---

## Testing Strategy

### Unit Tests
- Service methods with mocked dependencies
- Algorithm correctness

### Integration Tests
- Module-to-module event flow
- Database aggregation

### End-to-End Tests
- Full cycle: Booking → Analytics → Revenue → Channels

```csharp
[Test]
public async Task BookingCreated_ShouldUpdateAnalytics_ThenTriggerRevenueRecommendation()
{
    // 1. Create booking
    var booking = await bookingService.CreateAsync(request);
    
    // 2. Verify analytics updated
    var forecast = await analyticsService.GetOccupancyForecast(propertyId);
    Assert.IsTrue(forecast.PredictedBookedRooms > previousForecast);
    
    // 3. Verify revenue recommendation generated
    var recommendation = await revenueService.GenerateRecommendation(propertyId);
    Assert.IsNotNull(recommendation);
}
```

---

## Deployment & Scaling

### Development
- Single instance
- In-memory event bus
- All modules enabled

### Production
- **Monolith on single VM**: All modules same process
- **Scaled monolith**: Multiple instances + load balancer
- **Future**: Extract high-load modules to separate services

### Module Dependencies
```
Core ←── (no dependencies)
Events ←─ (references nothing)
Analytics ← reads Events, subscribes to all events
Channels ← reads Events, subscribes to booking/rate events
Revenue ← reads Events, depends on Analytics results
Operations ← reads Events
```

**Safe to extract**: Any module that depends only on Events

---

## Key Success Factors

### 1. **Enforce Loose Coupling**
- ✅ No direct DB queries across modules
- ✅ Events only, no method calls
- ✅ Interfaces for all external dependencies

### 2. **Keep It Simple**
- ✅ Background jobs > complex orchestration
- ✅ Eventual consistency > distributed transactions
- ✅ Metadata > hardcoded logic

### 3. **Monitor Data Quality**
- ✅ Audit log all state changes
- ✅ Regular reconciliation jobs
- ✅ Detect duplicate events early

### 4. **Plan for Failure**
- ✅ Idempotent event handlers
- ✅ Retry logic with exponential backoff
- ✅ Dead-letter queues for failed events

---

## Next Steps (Priority Order)

### Quarter 1
1. ✅ Analytics Module (occupancy forecasting)
2. ✅ Events Module (messaging infrastructure)
3. ✅ Channels Module (OTA sync)
4. ✅ Revenue Module (pricing recommendations)

### Quarter 2
5. OnlineBooking Module (embeddable widget)
6. Operations Module (POS, Maintenance)
7. Compliance Module (SARS, B-BBEE, POPIA)

### Quarter 3
8. Energy Module (IoT, load shedding)
9. Safari Module (Wildlife, Vehicle Management)
10. GuestApp Module (Mobile experience)

### Quarter 4
11. CRM Integration
12. Digital Keys
13. Guest Experience Platform

---

## Architecture Review Checklist (Quarterly)

- [ ] No module has direct coupling to another module's DB
- [ ] All async calls use correct exception handling
- [ ] Event subscriptions idempotent
- [ ] No hardcoded schema changes in reports
- [ ] Data duplication audit passed
- [ ] Load test completed (target: 1000 concurrent users)
- [ ] POPIA compliance audit
- [ ] Documentation updated

---

## Contact & Questions

**Lead Architect**: [Your Name]
**Architecture Review Board**: Quarterly
**Documentation**: Keep in sync with implementation

*Last Updated: 2026-02-10*
