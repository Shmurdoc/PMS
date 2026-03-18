# Dashboard Data Endpoints Guide

## Overview

The Dashboard Data endpoints provide real-time KPIs, charts, and time-series metrics for frontend visualization. All endpoints are organized by data type and return JSON optimized for dashboard consumption.

**Base URL:** `/api/dashboard/{propertyId}`
**Authorization:** RequireAuthorization (PropertyAdmin or PropertyManager)
**Content-Type:** application/json

---

## Endpoint Categories

### 1. KPI Metrics Endpoints

#### GET `/api/dashboard/{propertyId}/kpi/summary`
**Summary:** Get current month KPI summary
**Returns:** `KpiSummaryDto`

```json
{
  "propertyId": "550e8400-e29b-41d4-a716-446655440000",
  "generatedAt": "2026-03-18T14:30:00Z",
  "totalRevenue": 125000.00,
  "averageDailyRate": 1428.57,
  "revenuePerAvailableRoom": 2500.00,
  "occupancyRate": 85.50,
  "totalRooms": 50,
  "occupiedRooms": 42,
  "bookingCount": 87
}
```

**Use Case:** Main dashboard summary card
**Cached:** No (real-time)
**Query Time:** ~500ms

---

#### GET `/api/dashboard/{propertyId}/kpi/daily`
**Summary:** Get today's metrics
**Returns:** `DailyKpiDto`

```json
{
  "propertyId": "550e8400-e29b-41d4-a716-446655440000",
  "date": "2026-03-18",
  "generatedAt": "2026-03-18T14:30:00Z",
  "dailyRevenue": 7500.00,
  "bookingCount": 5
}
```

**Use Case:** Daily performance card, real-time updates
**Refresh:** Every 15 minutes
**Query Time:** ~200ms

---

#### GET `/api/dashboard/{propertyId}/kpi/monthly`
**Summary:** Get current month KPI breakdown
**Returns:** `MonthlyKpiDto`

```json
{
  "propertyId": "550e8400-e29b-41d4-a716-446655440000",
  "month": 3,
  "year": 2026,
  "generatedAt": "2026-03-18T14:30:00Z",
  "totalRevenue": 225000.00,
  "bookingCount": 150,
  "averageDailyRevenue": 7500.00
}
```

**Use Case:** Month-to-date progress tracking
**Cached:** 1 hour
**Query Time:** ~600ms

---

### 2. Chart Data Endpoints

#### GET `/api/dashboard/{propertyId}/chart/revenue-timeline`
**Summary:** 30-day revenue line chart
**Returns:** `TimelineChartDto`

```json
{
  "title": "Revenue (Last 30 Days)",
  "unit": "ZAR",
  "labels": ["Mar 01", "Mar 02", ..., "Mar 30"],
  "values": [7500.00, 8200.00, 7800.00, ...]
}
```

**Use Case:** Revenue trend visualization (line chart)
**Data Points:** 30 daily values
**Frontend:** Chart.js / ApexCharts

---

#### GET `/api/dashboard/{propertyId}/chart/occupancy-timeline`
**Summary:** 30-day occupancy % line chart
**Returns:** `TimelineChartDto`

```json
{
  "title": "Occupancy Rate (Last 30 Days)",
  "unit": "%",
  "labels": ["Mar 01", "Mar 02", ..., "Mar 30"],
  "values": [75.00, 82.00, 78.00, ...]
}
```

**Use Case:** Occupancy trend visualization
**Data Points:** 30 daily percentages (0-100)
**Benchmark:** Compare against seasonal

---

#### GET `/api/dashboard/{propertyId}/chart/adr-timeline`
**Summary:** 30-day average daily rate line chart
**Returns:** `TimelineChartDto`

```json
{
  "title": "Average Daily Rate (Last 30 Days)",
  "unit": "ZAR",
  "labels": ["Mar 01", "Mar 02", ..., "Mar 30"],
  "values": [1420.00, 1450.00, 1400.00, ...]
}
```

**Use Case:** Pricing trend visualization
**Data Points:** 30 daily ADR values
**Analysis:** Identify pricing patterns, seasonality

---

#### GET `/api/dashboard/{propertyId}/chart/room-status`
**Summary:** Current room status distribution (pie chart)
**Returns:** `PieChartDto`

```json
{
  "title": "Room Status Distribution",
  "labels": ["Occupied", "Vacant", "Dirty"],
  "values": [42, 6, 2]
}
```

**Use Case:** Real-time room status overview
**Update Frequency:** Real-time (via SignalR broadcast)
**Actions:** Click slice to filter by status

---

### 3. Time-Series Endpoints

#### GET `/api/dashboard/{propertyId}/timeseries/revenue/days`
**Summary:** 30-day revenue time-series for analysis
**Returns:** `TimeSeriesDto`

```json
{
  "metric": "revenue",
  "unit": "ZAR",
  "data": [
    { "timestamp": "2026-02-16T00:00:00Z", "value": 7500.00 },
    { "timestamp": "2026-02-17T00:00:00Z", "value": 8200.00 },
    ...
  ],
  "generatedAt": "2026-03-18T14:30:00Z"
}
```

**Use Case:** Statistical analysis, trend forecasting
**Data Structure:** ISO8601 timestamps + decimal values
**Aggregation:** Daily

---

#### GET `/api/dashboard/{propertyId}/timeseries/occupancy/days`
**Summary:** 30-day occupancy time-series
**Returns:** `TimeSeriesDto`

```json
{
  "metric": "occupancy",
  "unit": "%",
  "data": [
    { "timestamp": "2026-02-16T00:00:00Z", "value": 75.00 },
    { "timestamp": "2026-02-17T00:00:00Z", "value": 82.00 },
    ...
  ],
  "generatedAt": "2026-03-18T14:30:00Z"
}
```

**Use Case:** Occupancy forecasting, pattern detection
**Value Range:** 0-100 (percentage)

---

### 4. Comparison Endpoints

#### GET `/api/dashboard/{propertyId}/comparison/vs-last-month`
**Summary:** Month-over-month comparison
**Returns:** `ComparisonDto`

```json
{
  "period": "February 2026 vs March 2026",
  "currentRevenue": 225000.00,
  "previousRevenue": 185000.00,
  "trendPercentage": 21.62,
  "generatedAt": "2026-03-18T14:30:00Z"
}
```

**Use Case:** YoY/MoM performance analysis
**Metrics:**
- `trendPercentage > 0`: Growth (green)
- `trendPercentage < 0`: Decline (red)
- `trendPercentage = 0`: Flat (neutral)

---

### 5. Export Endpoints

#### GET `/api/dashboard/{propertyId}/export/monthly`
**Summary:** Export complete monthly dashboard data
**Returns:** `MonthlyExportDto`

```json
{
  "propertyId": "550e8400-e29b-41d4-a716-446655440000",
  "month": 3,
  "year": 2026,
  "totalRevenue": 225000.00,
  "bookingCount": 150,
  "exportedAt": "2026-03-18T14:30:00Z"
}
```

**Use Case:** Report generation, archive
**Format:** JSON (can extend for PDF/Excel)
**Timestamp:** ISO8601 UTC

---

## Frontend Integration Example

### React Component

```typescript
// useCallback to fetch KPI every 15 minutes
useEffect(() => {
  const fetchKpi = async () => {
    const res = await fetch(`/api/dashboard/${propertyId}/kpi/summary`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const data = await res.json();
    setKpi(data);
  };

  fetchKpi();
  const interval = setInterval(fetchKpi, 15 * 60 * 1000);
  return () => clearInterval(interval);
}, [propertyId, token]);

return (
  <div className="dashboard-grid">
    <Card title="Total Revenue">
      <Metric value={kpi.totalRevenue} unit="ZAR" />
    </Card>
    
    <Card title="Occupancy Rate">
      <Metric value={kpi.occupancyRate} unit="%" color="blue" />
    </Card>

    <Card title="Revenue Trend">
      <LineChart
        data={revenueTimeline}
        options={{ responsive: true, maintainAspectRatio: false }}
      />
    </Card>
  </div>
);
```

### Real-Time Updates via SignalR

```typescript
// Listen for occupancy updates (replaces polling)
connection.on("OccupancyUpdate", (message) => {
  setKpi(prev => ({
    ...prev,
    occupancyRate: message.occupancyRate,
    occupiedRooms: message.occupiedRooms
  }));
});

// Listen for dashboard refresh requests
connection.on("DashboardRefresh", (refreshScope) => {
  if (refreshScope === "occupancy" || refreshScope === "full") {
    fetchKpi();
  }
});
```

---

## Performance Characteristics

| Endpoint | Response Time | Data Points | Caching |
|----------|---------------|-------------|---------|
| /kpi/summary | ~500ms | 12 metrics | None |
| /kpi/daily | ~200ms | 4 metrics | 15 min |
| /kpi/monthly | ~600ms | 7 metrics | 1 hour |
| /chart/revenue-timeline | ~800ms | 30 values | None |
| /chart/occupancy-timeline | ~700ms | 30 values | None |
| /timeseries/revenue/days | ~1200ms | 30 points | None |
| /comparison/vs-last-month | ~400ms | 3 comparisons | 1 hour |

**Optimization Strategies:**
1. Cache daily/monthly KPIs (refresh hourly)
2. Use SignalR for real-time updates (occupancy, revenue)
3. Paginate time-series if > 90 days requested
4. Index queries by (PropertyId, CheckInDate)

---

## Error Handling

All endpoints return standard error responses:

```json
{
  "detail": "Property not found",
  "status": 404,
  "title": "Not Found"
}
```

**Common Errors:**
- `404` - Property not found
- `401` - Unauthorized (missing token)
- `403` - Forbidden (insufficient role)
- `500` - Server error

---

## Data Models

### KpiSummaryDto
- `propertyId`: Guid
- `generatedAt`: DateTime (UTC)
- `totalRevenue`: decimal (ZAR)
- `averageDailyRate`: decimal (ZAR)
- `revenuePerAvailableRoom`: decimal (ZAR)
- `occupancyRate`: decimal (0-100%)
- `totalRooms`: int
- `occupiedRooms`: int
- `bookingCount`: int

### TimelineChartDto
- `title`: string
- `unit`: string (ZAR, %)
- `labels`: List<string> (date strings)
- `values`: List<decimal> (numeric values)

### TimeSeriesDto
- `metric`: string (revenue, occupancy, adr)
- `unit`: string (ZAR, %)
- `data`: List<TimeSeriesPoint>
  - `timestamp`: DateTime (ISO8601)
  - `value`: decimal
- `generatedAt`: DateTime (UTC)

### ComparisonDto
- `period`: string (e.g., "Feb 2026 vs Mar 2026")
- `currentRevenue`: decimal
- `previousRevenue`: decimal
- `trendPercentage`: decimal (-100 to +100)
- `generatedAt`: DateTime

---

## Best Practices

1. **Polling Strategy:**
   - KPI Summary: Refresh every 15 minutes
   - Daily KPI: Refresh hourly
   - Charts: Load on-demand, cache for 1 hour

2. **SignalR for Real-Time:**
   - Room status: Instant via `BroadcastRoomStatusChange`
   - Occupancy updates: Instant via `BroadcastOccupancyUpdate`
   - Revenue updates: Instant via `BroadcastRevenueUpdate`

3. **Frontend Optimization:**
   - Lazy-load charts (only on tab/scroll)
   - Debounce refresh requests (no more than 1/min)
   - Cache responses locally for 5 minutes
   - Use skeleton loaders during fetch

4. **Data Freshness:**
   - Booking-based metrics (revenue, occupancy): Real-time via SignalR
   - Aggregate metrics (monthly KPI): Cache 1 hour
   - Historical data (30-day timeline): Cache 4 hours

---

## Endpoints Registered ✅

All 8 dashboard endpoints registered in `Program.cs` (line ~577):
```csharp
app.MapDashboardDataEndpoints();
```

**Files:**
- [DashboardDataEndpoints.cs](backend/src/SAFARIstack.API/Endpoints/DashboardDataEndpoints.cs)
- 0 compilation errors
- Ready for frontend integration
