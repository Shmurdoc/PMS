using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/folios/* and /api/payments/* endpoints — folio creation,
/// charge posting, payment recording, folio closing, revenue queries.
/// Verifies end-to-end financial data flow through the HTTP pipeline.
/// </summary>
[Collection("WebApp")]
public class FinancialEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public FinancialEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    private async Task<string> CreateBookingAndGetId(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/bookings", new
        {
            PropertyId = _factory.PropertyAId,
            GuestId = _factory.GuestAId,
            CheckInDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CheckOutDate = DateTime.UtcNow.AddDays(33).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            AdultCount = 2,
            ChildCount = 0,
            Rooms = new[]
            {
                new { RoomId = _factory.RoomAId, RoomTypeId = _factory.RoomTypeAId, RateApplied = 2500m }
            }
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("bookingId").GetString()!;
    }

    // ═══════════════════════════════════════════════════════════════
    //  FOLIO ENDPOINTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateFolio_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var bookingId = await CreateBookingAndGetId(client);

        var response = await client.PostAsJsonAsync("/api/folios", new
        {
            PropertyId = _factory.PropertyAId,
            BookingId = bookingId,
            GuestId = _factory.GuestAId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("folioNumber");
    }

    [Fact]
    public async Task AddCharge_ToFolio_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var bookingId = await CreateBookingAndGetId(client);

        // Create folio
        var folioResp = await client.PostAsJsonAsync("/api/folios", new
        {
            PropertyId = _factory.PropertyAId,
            BookingId = bookingId,
            GuestId = _factory.GuestAId
        });
        folioResp.EnsureSuccessStatusCode();
        var folio = await folioResp.Content.ReadFromJsonAsync<JsonElement>();
        var folioId = folio.GetProperty("id").GetString();

        // Add charge
        var chargeResp = await client.PostAsJsonAsync($"/api/folios/{folioId}/charges", new
        {
            Description = "Room Service - Dinner",
            Amount = 350.00m,
            Category = 2, // ChargeCategory.Breakfast
            Quantity = 1
        });

        var chargeBody = await chargeResp.Content.ReadAsStringAsync();
        chargeResp.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Response: {chargeBody}");
        chargeBody.Should().Contain("Room Service");
    }

    [Fact]
    public async Task GetFolioByBooking_AfterCreate_ReturnsData()
    {
        var client = _factory.CreateAuthenticatedClient();
        var bookingId = await CreateBookingAndGetId(client);

        // Create folio
        await client.PostAsJsonAsync("/api/folios", new
        {
            PropertyId = _factory.PropertyAId,
            BookingId = bookingId,
            GuestId = _factory.GuestAId
        });

        // Get folio
        var response = await client.GetAsync($"/api/folios/booking/{bookingId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOpenFolios_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/folios/open/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    // ═══════════════════════════════════════════════════════════════
    //  PAYMENT ENDPOINTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RecordPayment_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var bookingId = await CreateBookingAndGetId(client);

        // Create folio
        var folioResp = await client.PostAsJsonAsync("/api/folios", new
        {
            PropertyId = _factory.PropertyAId,
            BookingId = bookingId,
            GuestId = _factory.GuestAId
        });
        folioResp.EnsureSuccessStatusCode();
        var folio = await folioResp.Content.ReadFromJsonAsync<JsonElement>();
        var folioId = folio.GetProperty("id").GetString();

        // Record payment
        var payResp = await client.PostAsJsonAsync("/api/payments", new
        {
            PropertyId = _factory.PropertyAId,
            FolioId = folioId,
            Amount = 1000.00m,
            Method = 0, // PaymentMethod value
            TransactionReference = $"TXN-{Guid.NewGuid():N}"[..20],
            BookingId = bookingId
        });

        var payBody = await payResp.Content.ReadAsStringAsync();
        payResp.StatusCode.Should().Be(HttpStatusCode.Created, because: $"Response: {payBody}");
        payBody.Should().Contain("transactionReference");
    }

    [Fact]
    public async Task GetPaymentsByFolio_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var bookingId = await CreateBookingAndGetId(client);

        // Create folio
        var folioResp = await client.PostAsJsonAsync("/api/folios", new
        {
            PropertyId = _factory.PropertyAId,
            BookingId = bookingId,
            GuestId = _factory.GuestAId
        });
        var folio = await folioResp.Content.ReadFromJsonAsync<JsonElement>();
        var folioId = folio.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/payments/folio/{folioId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task GetTotalRevenue_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var to = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await client.GetAsync(
            $"/api/payments/revenue/{_factory.PropertyAId}?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("totalRevenue");
    }

    [Fact]
    public async Task CloseFolio_WithNoBalance_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient();
        var bookingId = await CreateBookingAndGetId(client);

        // Create folio (empty = zero balance)
        var folioResp = await client.PostAsJsonAsync("/api/folios", new
        {
            PropertyId = _factory.PropertyAId,
            BookingId = bookingId,
            GuestId = _factory.GuestAId
        });
        var folio = await folioResp.Content.ReadFromJsonAsync<JsonElement>();
        var folioId = folio.GetProperty("id").GetString();

        // Close it (zero balance should succeed)
        var closeResp = await client.PostAsync($"/api/folios/{folioId}/close?userId={_factory.UserAId}", null);
        closeResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RecordPayment_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            PropertyId = _factory.PropertyAId,
            FolioId = Guid.NewGuid(),
            Amount = 100m,
            Method = 0
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
