# SAFARIstack PMS - Architecture Documentation

## System Overview

SAFARIstack PMS is a production-ready lodge management system built with:
- **ASP.NET Core 8 Minimal API**
- **Clean Architecture** with **Modular Monolith** design
- **PostgreSQL** (Supabase) with **EF Core Code-First**
- **MediatR** for CQRS and event-driven architecture
- **Strong typing** with UUID identifiers
- **SA-specific compliance**: ZAR currency, 15% VAT, 1% Tourism Levy, BCEA labor laws

## Architecture Layers

```
┌─────────────────────────────────────────────┐
│           API Layer (Minimal API)           │
│   - HTTP Endpoints                          │
│   - Authentication/Authorization            │
│   - Request/Response handling               │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│         Application Layer (CQRS)            │
│   - Commands & Queries (MediatR)            │
│   - Handlers                                │
│   - DTOs                                    │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│            Domain Layer                     │
│   - Entities & Aggregates                   │
│   - Value Objects                           │
│   - Domain Events                           │
│   - Business Rules                          │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│         Infrastructure Layer                │
│   - EF Core DbContext                       │
│   - Repositories                            │
│   - External Services                       │
│   - Edge Buffering                          │
└─────────────────────────────────────────────┘
```

## Module Structure

### Core Module (Bookings, Guests, Properties)
- **Domain**: Property, Guest, Booking, Room, RoomType
- **Application**: CreateBooking, GetBookingById, CheckIn, CheckOut
- **Features**:
  - SA financial compliance (VAT, Tourism Levy)
  - Multi-room bookings
  - Payment tracking
  - Audit trail

### Modules.Staff (RFID & Attendance)
- **Domain**: StaffMember, RfidCard, RfidReader, StaffAttendance
- **Application**: RfidCheckIn, RfidCheckOut, AttendanceReporting
- **Features**:
  - RFID hardware integration with X-Reader-API-Key auth
  - BCEA labor compliance (overtime, breaks, public holidays)
  - Velocity checks (anti-duplicate scanning)
  - Mobile check-in/out with GPS
  - Edge buffering for offline resilience

### Modules.Addons (Extensibility)
- **Purpose**: Future extensibility for:
  - Energy management (load shedding, solar)
  - Safari operations (game drives, wildlife tracking)
  - Restaurant/kitchen management
  - Additional property-specific features

## Authentication

### JWT Authentication (Users)
- Standard Bearer token authentication
- Used for: Staff portal, management interface
- Claims: UserId, PropertyId, Role
- Expiration: 60 minutes (configurable)

### X-Reader-API-Key Authentication (RFID Hardware)
- Custom authentication scheme for RFID readers
- Header: `X-Reader-API-Key: {api-key}`
- Used for: RFID check-in/out endpoints
- Features:
  - Per-reader API keys
  - Optional IP whitelisting
  - Velocity checks (5-second cooldown)
  - Reader heartbeat monitoring

## Event-Driven Architecture

All domain events flow through MediatR:

```csharp
// Domain Event
public record BookingCreatedEvent(Guid BookingId, string Reference) : DomainEvent;

// Event Handler (in any module)
public class SendBookingConfirmationHandler : INotificationHandler<BookingCreatedEvent>
{
    public async Task Handle(BookingCreatedEvent notification, CancellationToken ct)
    {
        // Send email, SMS, update cache, etc.
    }
}
```

**Inter-module communication**: Modules communicate via domain events, maintaining loose coupling.

## Network Resilience (Edge Buffering)

For offline scenarios (e.g., remote lodges with intermittent connectivity):

```csharp
// Operation buffered when network unavailable
await edgeBuffer.BufferOperationAsync(
    "CheckIn",
    "StaffAttendance",
    attendanceId,
    attendanceData);

// Automatically retried when connection restored
await edgeBuffer.ProcessBufferedOperationsAsync(processor);
```

## SA-Specific Features

### Financial Compliance
```csharp
var subtotal = Money.FromZAR(1000.00m);
var breakdown = FinancialBreakdown.Calculate(subtotal);

// Breakdown:
// Subtotal: R1,000.00
// VAT (15%): R150.00
// Tourism Levy (1%): R10.00
// Total: R1,160.00
```

### Labor Compliance (BCEA)
```csharp
var calculator = new SouthAfricanLaborCalculator();
var overtime = calculator.CalculateOvertime(checkIn, checkOut, scheduledHours, hourlyRate);

// Handles:
// - Daily overtime (>9 hours)
// - Sunday work (2x rate)
// - Public holidays (2x rate)
// - Night shift allowance (10%)
// - Required breaks
```

## Database Schema

Key tables:
- `properties`: Lodge/hotel information
- `guests`: Guest profiles with SA ID types
- `bookings`: Booking with financial breakdown
- `rooms`, `room_types`: Inventory management
- `staff_members`: Staff profiles
- `rfid_cards`: RFID card assignments
- `rfid_readers`: Reader registration
- `staff_attendance`: Time & attendance with wage calculation

All tables use:
- UUID primary keys (`id`)
- Timestamps (`created_at`, `updated_at`)
- Soft deletes where appropriate

## API Endpoints

### Core Bookings
- `POST /api/bookings` - Create booking
- `GET /api/bookings/{id}` - Get booking
- `POST /api/bookings/{id}/check-in` - Check-in
- `POST /api/bookings/{id}/check-out` - Check-out

### Staff Management
- `GET /api/staff/attendance/today` - Today's attendance
- `GET /api/staff/attendance/report` - Attendance report
- `POST /api/staff/overtime/request` - Request overtime
- `POST /api/staff/overtime/{id}/approve` - Approve overtime

### RFID Hardware (X-Reader-API-Key auth)
- `POST /api/rfid/check-in` - RFID check-in
- `POST /api/rfid/check-out` - RFID check-out
- `POST /api/rfid/heartbeat` - Reader heartbeat

## Extension Points

### Adding New Modules
1. Create new project: `SAFARIstack.Modules.{Name}`
2. Define domain entities in `Domain/Entities`
3. Create commands/queries in `Application`
4. Register in `Program.cs`
5. Add endpoints in `API/Endpoints`

### Adding New Features
1. Define domain event
2. Create command/query
3. Implement handler
4. Map endpoint
5. Add tests

## Performance Considerations

- **Indexes**: All foreign keys, frequently queried fields
- **Eager loading**: Use `.Include()` for related entities
- **Query filters**: Global filters for soft deletes, active records
- **Caching**: Redis for frequently accessed data (future)
- **Connection pooling**: Enabled by default in Npgsql

## Security

- **Authentication**: JWT + Custom RFID scheme
- **Authorization**: Role-based access control
- **Rate limiting**: Configured per endpoint
- **Velocity checks**: Prevent rapid duplicate operations
- **SQL injection**: Parameterized queries via EF Core
- **CORS**: Configurable per environment
- **HTTPS**: Required in production

## Testing Strategy

- **Unit tests**: Domain logic, calculations
- **Integration tests**: API endpoints, database operations
- **End-to-end tests**: Complete workflows
- **Load tests**: RFID endpoints (high frequency)

## Future Enhancements

- [ ] SignalR for real-time updates
- [ ] Background jobs (Hangfire) for scheduled tasks
- [ ] Redis caching layer
- [ ] Advanced reporting & analytics
- [ ] Mobile app for staff
- [ ] Integration with Eskom Se Push (load shedding)
- [ ] Payment gateway integration (PayFast, Yoco)
- [ ] Multi-property management
- [ ] Advanced BBBEE compliance tracking
