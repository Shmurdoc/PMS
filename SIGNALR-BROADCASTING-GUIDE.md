# SignalR Real-Time Broadcasting Guide

## Overview

The `ISignalRNotificationService` provides a convenient way for endpoints and services to broadcast real-time updates to connected clients via SignalR. It encapsulates all the complexity of working with `IHubContext<PmsHub>` and provides type-safe methods.

**Location:** `src/SAFARIstack.API/Services/SignalRNotificationService.cs`

## Architecture

### Components

1. **PmsHub.cs** - The SignalR hub with:
   - 15+ broadcast methods organized by domain
   - JWT authorization
   - Property-scoped groups for multi-tenancy
   - Type-safe message record classes

2. **ISignalRNotificationService** - The helper interface:
   - Server-side only (does NOT inherit Hub)
   - Uses `IHubContext<PmsHub>` internally
   - Injected into endpoints/services
   - Error handling and logging built-in

3. **SignalRNotificationService** - The implementation
   - Broadcasts to property groups
   - Automatic timestamp management
   - Comprehensive error handling
   - Logging for audit trail

## Registration

Already registered in `Program.cs` (line ~471):

```csharp
// Register SignalR notification service (real-time broadcast helper)
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
```

## Usage in Endpoints

### Inject into endpoint handler

```csharp
app.MapPost("/api/bookings", async (
    CreateBookingRequest req,
    BookingService bookingService,
    ISignalRNotificationService signalRService,  // ← Inject here
    CancellationToken ct) =>
{
    // Create booking
    var booking = await bookingService.CreateAsync(req, ct);

    // Broadcast to all clients in property group
    await signalRService.BroadcastBookingCreateAsync(
        propertyId: req.PropertyId,
        bookingId: booking.Id,
        guestName: booking.Guest.FullName,
        roomNumber: booking.Room.RoomNumber,
        totalPrice: booking.TotalPrice,
        checkIn: booking.CheckInDate,
        checkOut: booking.CheckOutDate);

    return Results.Created($"/api/bookings/{booking.Id}", booking);
});
```

### Available Methods by Domain

#### Booking Events

```csharp
// Create booking and notify clients
await signalRService.BroadcastBookingCreateAsync(
    propertyId, bookingId, guestName, roomNumber, totalPrice, checkIn, checkOut);

// Cancel booking
await signalRService.BroadcastBookingCancelAsync(
    propertyId, bookingId, guestName, reason, refundAmount);

// Guest checks in
await signalRService.BroadcastCheckInAsync(
    propertyId, bookingId, guestId, guestName, roomNumber, roomType);

// Guest checks out
await signalRService.BroadcastCheckOutAsync(
    propertyId, bookingId, guestId, guestName, roomNumber, finalBalance);

// Room changes status (dirty → clean, occupied → vacant, etc.)
await signalRService.BroadcastRoomStatusChangeAsync(
    propertyId, roomId, roomNumber, previousStatus, newStatus, notes: "reason");
```

#### Housekeeping Events

```csharp
// Task assigned to staff member
await signalRService.BroadcastHousekeepingTaskCreateAsync(
    propertyId, taskId, roomNumber, taskType, staffId, staffName);

// Task status changes (assigned → in_progress → completed)
await signalRService.BroadcastTaskStatusChangeAsync(
    propertyId, taskId, previousStatus, newStatus, staffId, completionNotes: "notes");

// Room inspection completed
await signalRService.BroadcastRoomInspectionAsync(
    propertyId, inspectionId, taskId, roomNumber, result, 
    issues: new List<string> { "stain on wall", "missing towel" }, staffId);
```

#### Staff Events

```csharp
// Staff member checks in (RFID or manual)
await signalRService.BroadcastStaffCheckInAsync(
    propertyId, staffId, staffName, location: "front desk", rfidTagId: "RFID123");

// Staff member checks out
await signalRService.BroadcastStaffCheckOutAsync(
    propertyId, staffId, staffName, location: "front desk");

// Staff shift changes or roster updated
await signalRService.BroadcastStaffShiftChangeAsync(
    propertyId, staffId, staffName, department, shiftStart, shiftEnd, position);

// Bulk roster update
await signalRService.BroadcastRosterUpdateAsync(
    propertyId, 
    new List<Guid> { staffId1, staffId2 }, 
    updateReason: "schedule_revision");
```

#### Payment/Revenue Events

```csharp
// Payment received
await signalRService.BroadcastPaymentReceivedAsync(
    propertyId, bookingId, amount: 1500.00m, currency: "ZAR");

// Refund issued
await signalRService.BroadcastRefundIssuedAsync(
    propertyId, bookingId, amount: 500.00m, currency: "ZAR");
```

#### Dashboard Events

```csharp
// Request dashboard refresh
await signalRService.RequestDashboardRefreshAsync(
    propertyId, 
    refreshScope: "occupancy");  // or "revenue", "staff", "full"

// Update occupancy metrics
await signalRService.BroadcastOccupancyUpdateAsync(
    propertyId, 
    totalRooms: 50,
    occupiedRooms: 35,
    vacantRooms: 10,
    dirtyRooms: 3,
    maintenanceRooms: 2);

// Update revenue metrics (for revenue chart)
await signalRService.BroadcastRevenueUpdateAsync(
    propertyId,
    date: DateTime.Today,
    dailyRevenue: 50000m,
    adr: 1428.57m,
    revPar: 1000m,
    checkIns: 25,
    checkOuts: 22);
```

#### General Notifications

```csharp
// Send arbitrary system notification
await signalRService.SendSystemNotificationAsync(
    propertyId,
    type: "system_alert",
    data: new { message = "Maintenance required", severity = "high" });
```

## End-to-End Example: Booking Creation

### Backend (Endpoint)

```csharp
app.MapPost("/api/bookings", async (
    CreateBookingRequest req,
    ApplicationDbContext db,
    ISignalRNotificationService signalRService,
    IValidator<CreateBookingRequest> validator,
    CancellationToken ct) =>
{
    // Validate
    var validationResult = await validator.ValidateAsync(req, ct);
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);

    // Create booking in database
    var guest = await db.Guests.FindAsync(new object[] { req.GuestId }, ct);
    if (guest is null)
        return Results.NotFound("Guest not found");

    var booking = Booking.Create(
        req.PropertyId, req.GuestId, req.RoomId, 
        req.CheckInDate, req.CheckOutDate, req.Notes);

    db.Bookings.Add(booking);
    await db.SaveChangesAsync(ct);

    // Get room details
    var room = await db.Rooms.FindAsync(new object[] { req.RoomId }, ct);

    // ✓ BROADCAST TO ALL CLIENTS IN PROPERTY GROUP
    await signalRService.BroadcastBookingCreateAsync(
        propertyId: req.PropertyId,
        bookingId: booking.Id,
        guestName: guest.FullName,
        roomNumber: room!.RoomNumber,
        totalPrice: booking.TotalPrice,
        checkIn: booking.CheckInDate,
        checkOut: booking.CheckOutDate);

    return Results.Created($"/api/bookings/{booking.Id}", new
    {
        booking.Id,
        booking.PropertyId,
        booking.CheckInDate,
        booking.CheckOutDate,
        GuestName = guest.FullName,
        RoomNumber = room.RoomNumber,
        booking.TotalPrice
    });
}).WithName("CreateBooking");
```

### Frontend (JavaScript/C# + SignalR Client)

```javascript
// JavaScript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/pms", { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();

connection.start().catch(err => console.error(err));

// Listen for booking updates
connection.on("BookingUpdate", (message) => {
    console.log("New booking:", message);
    // {
    //   bookingId: "guid",
    //   propertyId: "guid",
    //   guestName: "John Doe",
    //   status: "confirmed",
    //   checkInDate: "2024-03-15",
    //   checkOutDate: "2024-03-20",
    //   roomNumber: 101,
    //   totalPrice: 5000,
    //   updateReason: "new_booking",
    //   timestamp: "2024-03-10T14:30:00Z",
    //   eventType: "booking_create"
    // }
    
    // Update UI - refresh bookings list, show toast, etc.
    updateBookingsList();
    showNotification(`New booking from ${message.guestName}`);
});

// Also listen for other events
connection.on("RoomStatusChanged", (message) => {
    console.log("Room status updated:", message);
    updateRoomStatus(message.roomNumber, message.status);
});

connection.on("DashboardRefresh", (refreshScope) => {
    console.log("Dashboard refresh requested:", refreshScope);
    // Fetch fresh data: _analytics/occupancy, _analytics/revenue, etc.
});
```

## Error Handling

All broadcast methods include try-catch blocks with logging:

```csharp
try
{
    await _hubContext.Clients.Group($"property-{propertyId}").SendAsync("BookingUpdate", message);
    _logger.LogInformation("SignalR: Broadcast BookingCreate PropertyId={PropertyId}", propertyId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error broadcasting booking create");
}
```

**If a client is offline:**
- SignalR automatically disconnects
- Message is NOT delivered (not persisted)
- Client re-joins group on reconnection
- Server doesn't wait for offline clients

**For persistence:** Implement a separate notification queue (e.g., Hangfire, background service) if you need messages delivery guarantee.

## Multi-Tenancy (Property Isolation)

The service automatically isolates broadcasts to property groups:

```csharp
// Only broadcasts to clients with PmsHub connection in group "property-{propertyId}"
await _hubContext.Clients.Group($"property-{propertyId}").SendAsync(
    "BookingUpdate", message);
```

**Client-side:** Clients must join the property group after connecting:

```javascript
// On successful connection
connection.invoke("JoinPropertyGroup", propertyId)
    .catch(err => console.error("Failed to join property group:", err));

// On disconnect/logout
connection.invoke("LeavePropertyGroup", propertyId)
    .catch(err => console.error("Failed to leave property group:", err));
```

## Performance Considerations

1. **Broadcast Volume:** Each broadcast is async and non-blocking
2. **Groups:** Groups are memory-efficient (one list per group)
3. **Message Size:** Keep message payloads small (< 64KB per message)
4. **Frequency:** Don't broadcast more than 1-2 per second per event type (use debouncing on client)

## Testing

### Local Testing

```csharp
[Fact]
public async Task BroadcastBookingCreate_SendsMessageToPropertyGroup()
{
    // Arrange
    var propertyId = Guid.NewGuid();
    var mockHubContext = new Mock<IHubContext<PmsHub>>();
    var mockClients = new Mock<IHubClients>();
    var mockGroup = new Mock<IClientProxy>();
    
    mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
    mockClients.Setup(x => x.Group($"property-{propertyId}"))
        .Returns(mockGroup.Object);

    var service = new SignalRNotificationService(mockHubContext.Object, _logger);

    // Act
    await service.BroadcastBookingCreateAsync(
        propertyId, Guid.NewGuid(), "John Doe", 101, 5000m, 
        DateTime.Today, DateTime.Today.AddDays(5));

    // Assert
    mockGroup.Verify(x => x.SendCoreAsync(
        "BookingUpdate", 
        It.IsAny<object?[]>(), 
        It.IsAny<CancellationToken>()), 
        Times.Once);
}
```

## Integration Checklist

- [x] SignalRNotificationService interface defined
- [x] SignalRNotificationService implementation created (15+ methods)
- [x] PmsHub expanded with broadcast methods
- [x] Service registered in Program.cs (line ~471)
- [x] Message record types created (10 types)
- [x] Error handling & logging added
- [x] Documentation (this file)
- [ ] Integrate into all endpoints (Bookings, Payments, Staff, Housekeeping)
- [ ] Client-side connection example
- [ ] Integration tests
- [ ] Load testing (multi-client broadcasts)

## Next Steps

To integrate real-time updates into your endpoints:

1. **Inject `ISignalRNotificationService` into endpoint handlers**
2. **Call appropriate broadcast method after database operations**
3. **Client-side:** Listen for events using SignalR JavaScript library
4. **Frontend:** Update UI when broadcast messages arrive

**Example:** See "End-to-End Example: Booking Creation" above.
