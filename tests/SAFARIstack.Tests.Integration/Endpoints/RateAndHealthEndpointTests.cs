using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/rates/* endpoints — seasons, rate plans, effective rates.
/// Also tests /health endpoint and cross-cutting concerns.
/// </summary>
[Collection("WebApp")]
public class RateAndHealthEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public RateAndHealthEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    // ═══════════════════════════════════════════════════════════════
    //  RATE / SEASON ENDPOINTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateSeason_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/rates/seasons", new
        {
            PropertyId = _factory.PropertyAId,
            Name = "Peak Season 2026",
            Code = "PEAK2026",
            Type = 0, // SeasonType enum
            StartDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            EndDate = new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            PriceMultiplier = 1.5m,
            Priority = 10
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Peak Season");
    }

    [Fact]
    public async Task GetSeasonsByProperty_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/rates/seasons/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRatePlans_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/rates/plans/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEffectiveRate_NoMatch_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await client.GetAsync(
            $"/api/rates/effective?roomTypeId={Guid.NewGuid()}&ratePlanId={Guid.NewGuid()}&date={date}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRatesByRoomType_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var from = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var to = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await client.GetAsync(
            $"/api/rates/room-type/{_factory.RoomTypeAId}?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ═══════════════════════════════════════════════════════════════
    //  HEALTH ENDPOINT
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Health_Returns200WithDatabaseStatus()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
        body.Should().ContainEquivalentOf("postgresql");
    }

    // ═══════════════════════════════════════════════════════════════
    //  ROLES & PERMISSIONS ENDPOINT
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllRoles_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/roles/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("SuperAdmin");
    }

    [Fact]
    public async Task GetAllPermissions_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/roles/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRoles_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/roles/");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════
    //  SECURITY HEADERS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllResponses_ContainSecurityHeaders()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/health");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }
}
