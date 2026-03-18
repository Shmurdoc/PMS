# SAFARIstack PMS - API Testing Guide

## Quick Start

### 1. Start the API
```bash
cd src/SAFARIstack.API
dotnet run
```

API runs at: https://localhost:7001

### 2. Access Swagger UI
Navigate to: https://localhost:7001/swagger

---

## Authentication Testing

### JWT Authentication (Users)
Not yet implemented - requires user registration/login endpoints.
For testing, you can create a mock JWT token or temporarily disable auth on specific endpoints.

### RFID Reader Authentication

**Headers Required:**
```
X-Reader-API-Key: your-api-key-here
```

---

## Core API Tests

### Health Check
```bash
curl https://localhost:7001/health
```

**Expected Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2026-02-09T10:30:00Z",
  "version": "1.0.0"
}
```

---

## Booking Endpoints

### Create Booking
```bash
curl -X POST https://localhost:7001/api/bookings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {your-jwt-token}" \
  -d '{
    "propertyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "guestId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "checkInDate": "2026-03-01",
    "checkOutDate": "2026-03-05",
    "adultCount": 2,
    "childCount": 0,
    "rooms": [
      {
        "roomId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
        "roomTypeId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
        "rateApplied": 1500.00
      }
    ],
    "specialRequests": "Late check-in requested"
  }'
```

**Expected Response:**
```json
{
  "bookingId": "generated-uuid",
  "bookingReference": "BK-20260209-1234",
  "totalAmount": 6900.00,
  "success": true
}
```

**Financial Breakdown:**
- Subtotal: R1,500 × 4 nights = R6,000
- VAT (15%): R900
- Tourism Levy (1%): R60
- **Total: R6,960**

### Get Booking
```bash
curl https://localhost:7001/api/bookings/{booking-id} \
  -H "Authorization: Bearer {your-jwt-token}"
```

---

## RFID Hardware Endpoints

### RFID Check-In
```bash
curl -X POST https://localhost:7001/api/rfid/check-in \
  -H "Content-Type: application/json" \
  -H "X-Reader-API-Key: reader-api-key-123" \
  -d '{
    "cardUid": "ABC123DEF456",
    "readerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }'
```

**Success Response:**
```json
{
  "success": true,
  "attendanceId": "generated-uuid",
  "staffName": "John Doe",
  "checkInTime": "2026-02-09T08:00:00Z",
  "message": "Check-in successful"
}
```

**Error Responses:**

**Card Not Found (404):**
```json
{
  "success": false,
  "attendanceId": null,
  "staffName": null,
  "checkInTime": null,
  "message": "Card not recognized",
  "errorCode": "CARD_NOT_FOUND"
}
```

**Already Checked In (400):**
```json
{
  "success": false,
  "attendanceId": "existing-uuid",
  "staffName": "John Doe",
  "checkInTime": "2026-02-09T07:30:00Z",
  "message": "Already checked in",
  "errorCode": "ALREADY_CHECKED_IN"
}
```

**Velocity Check Failed (429):**
```json
{
  "success": false,
  "message": "Duplicate scan detected. Please wait a few seconds.",
  "errorCode": "VELOCITY_CHECK_FAILED"
}
```

### RFID Check-Out
```bash
curl -X POST https://localhost:7001/api/rfid/check-out \
  -H "Content-Type: application/json" \
  -H "X-Reader-API-Key: reader-api-key-123" \
  -d '{
    "cardUid": "ABC123DEF456",
    "readerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }'
```

**Success Response:**
```json
{
  "success": true,
  "attendanceId": "uuid",
  "staffName": "John Doe",
  "checkOutTime": "2026-02-09T17:00:00Z",
  "totalHours": 9.0,
  "overtimeHours": 0,
  "totalWage": 1350.00,
  "message": "Check-out successful"
}
```

**With Overtime:**
```json
{
  "success": true,
  "attendanceId": "uuid",
  "staffName": "John Doe",
  "checkOutTime": "2026-02-09T19:30:00Z",
  "totalHours": 11.5,
  "overtimeHours": 2.5,
  "totalWage": 1912.50,
  "message": "Check-out successful"
}
```

**Calculation:**
- Regular hours: 9 × R150 = R1,350
- Overtime hours: 2.5 × R225 (1.5x) = R562.50
- **Total: R1,912.50**

### RFID Reader Heartbeat
```bash
curl -X POST https://localhost:7001/api/rfid/heartbeat \
  -H "Content-Type: application/json" \
  -H "X-Reader-API-Key: reader-api-key-123" \
  -d '{
    "readerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "readerSerial": "READER-001",
    "status": "Online"
  }'
```

---

## Staff Management Endpoints

### Get Today's Attendance
```bash
curl https://localhost:7001/api/staff/attendance/today?propertyId={property-id} \
  -H "Authorization: Bearer {your-jwt-token}"
```

### Get Attendance Report
```bash
curl "https://localhost:7001/api/staff/attendance/report?propertyId={id}&startDate=2026-02-01&endDate=2026-02-09" \
  -H "Authorization: Bearer {your-jwt-token}"
```

---

## Error Handling

All endpoints return consistent error responses:

```json
{
  "success": false,
  "message": "Human-readable error message",
  "errorCode": "MACHINE_READABLE_CODE",
  "details": {
    "additionalInfo": "value"
  }
}
```

**Common Error Codes:**
- `CARD_NOT_FOUND` - RFID card not registered
- `CARD_INACTIVE` - Card is deactivated/lost/stolen
- `ALREADY_CHECKED_IN` - Staff already has active check-in
- `NOT_CHECKED_IN` - No active check-in for check-out
- `VELOCITY_CHECK_FAILED` - Duplicate scan within cooldown period
- `INVALID_API_KEY` - Invalid X-Reader-API-Key
- `READER_NOT_FOUND` - RFID reader not registered
- `INTERNAL_ERROR` - Unexpected server error

---

## Testing Scenarios

### Scenario 1: Normal Staff Day
1. **08:00** - Check-in via RFID
2. **12:00** - Start break (optional, tracked separately)
3. **12:30** - End break
4. **17:00** - Check-out via RFID
   - Result: 8.5 hours worked, 0.5 break = 8 hours paid

### Scenario 2: Overtime Day
1. **08:00** - Check-in via RFID
2. **19:00** - Check-out via RFID
   - Result: 11 hours worked, 2 hours overtime @ 1.5x rate

### Scenario 3: Sunday Work (BCEA Compliance)
1. **Sunday 08:00** - Check-in
2. **Sunday 16:00** - Check-out
   - Result: 8 hours @ 2x rate (double time)

### Scenario 4: Duplicate Scan Prevention
1. **08:00:00** - Check-in via RFID ✅
2. **08:00:03** - Same card scans again ❌ (Velocity check fails)
3. **08:00:10** - Can scan again ✅

### Scenario 5: Lost Card
1. Mark card as "Lost" in system
2. Attempt check-in
   - Result: Error "Card is lost"

---

## Performance Testing

### RFID Endpoint Load Test
RFID readers can scan frequently. Test with:

```bash
# Using Apache Bench
ab -n 1000 -c 10 -H "X-Reader-API-Key: test-key" \
  -p checkin.json -T application/json \
  https://localhost:7001/api/rfid/check-in

# Expected: <100ms response time, 0% error rate
```

---

## Database Verification

After operations, verify database state:

```sql
-- Check attendance record
SELECT * FROM staff_attendance 
WHERE card_uid = 'ABC123DEF456' 
ORDER BY created_at DESC 
LIMIT 1;

-- Verify overtime calculation
SELECT 
  staff_id,
  check_in_time,
  check_out_time,
  actual_hours,
  overtime_hours,
  total_wage
FROM staff_attendance
WHERE check_out_time IS NOT NULL;

-- Check booking financials
SELECT 
  booking_reference,
  subtotal_amount,
  vat_amount,
  tourism_levy,
  total_amount
FROM bookings;
```

---

## Troubleshooting

### Issue: "Card not recognized"
- Verify card exists in `rfid_cards` table
- Check card status is "Active"
- Verify `card_uid` matches exactly (case-sensitive)

### Issue: "Invalid API key"
- Verify reader registered in `rfid_readers` table
- Check `api_key` in database matches header
- Ensure reader status is "Active"

### Issue: Connection errors
- Verify PostgreSQL is running
- Check connection string in `appsettings.json`
- Run `dotnet ef database update` if migrations pending

### Issue: Swagger not loading
- Clear browser cache
- Verify API is running on correct port
- Check `launchSettings.json` configuration

---

## Next Steps

1. **Set up database**: Run EF Core migrations
2. **Seed test data**: Create properties, staff, rooms
3. **Register RFID readers**: Add test readers with API keys
4. **Issue RFID cards**: Assign cards to staff members
5. **Test workflows**: Run through scenarios above
6. **Monitor logs**: Check `logs/` directory for issues

---

## Support Resources

- **Swagger UI**: https://localhost:7001/swagger
- **Health Check**: https://localhost:7001/health
- **Logs**: `logs/safaristack-{date}.log`
- **Architecture**: See `ARCHITECTURE.md`
- **Deployment**: See `DEPLOYMENT.md`
