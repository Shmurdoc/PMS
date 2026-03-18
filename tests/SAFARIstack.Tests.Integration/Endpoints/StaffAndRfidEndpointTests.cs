using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/staff/* and /api/rfid/* endpoints — attendance,
/// staff listing, overtime, RFID check-in/out, heartbeat.
/// </summary>
[Collection("WebApp")]
public class StaffAndRfidEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public StaffAndRfidEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    // ═══════════════════════════════════════════════════════════════
    //  STAFF ENDPOINTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStaffByProperty_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/staff/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task GetTodayAttendance_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync(
            $"/api/staff/attendance/today?propertyId={_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task GetAttendanceReport_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var start = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var end = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await client.GetAsync(
            $"/api/staff/attendance/report?propertyId={_factory.PropertyAId}&startDate={start}&endDate={end}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestOvertime_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/staff/overtime/request", new
        {
            StaffMemberId = _factory.StaffAId,
            Date = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Hours = 3.0m,
            Reason = "Coverage for sick colleague"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("PendingApproval");
    }

    [Fact]
    public async Task ApproveOvertime_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync($"/api/staff/overtime/{Guid.NewGuid()}/approve", new
        {
            ApprovedByUserId = _factory.UserAId,
            Approved = true,
            Notes = "Approved for coverage"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Approved");
    }

    [Fact]
    public async Task GetStaff_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/staff/{_factory.PropertyAId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RFID ENDPOINTS (require X-Reader-API-Key)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RfidCheckIn_WithoutApiKey_Returns401()
    {
        // RFID endpoints require X-Reader-API-Key, not JWT
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/rfid/check-in", new
        {
            CardUid = "CARD-001",
            ReaderId = (Guid?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RfidHeartbeat_WithoutApiKey_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/rfid/heartbeat", new
        {
            ReaderId = Guid.NewGuid(),
            ReaderSerial = "RDR-001",
            Status = "Online"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
