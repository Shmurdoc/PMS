using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/bookings/* endpoints — create booking via MediatR,
/// get by ID, get by property, check-in, check-out.
/// Verifies the full MediatR pipeline: HTTP → command → handler → repo → DB.
/// </summary>
[Collection("WebApp")]
public class BookingEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public BookingEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateBooking_ValidRequest_Returns201WithBookingRef()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            GuestId = _factory.GuestAId,
            CheckInDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CheckOutDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            AdultCount = 2,
            ChildCount = 1,
            Rooms = new[]
            {
                new { RoomId = _factory.RoomAId, RoomTypeId = _factory.RoomTypeAId, RateApplied = 2500m }
            },
            SpecialRequests = "Extra pillows",
            CreatedByUserId = _factory.UserAId
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("bookingId");
        body.Should().Contain("bookingReference");
    }

    [Fact]
    public async Task CreateBooking_MissingGuest_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            GuestId = Guid.Empty,
            CheckInDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CheckOutDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            AdultCount = 2,
            ChildCount = 0,
            Rooms = Array.Empty<object>()
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBooking_CheckOutBeforeCheckIn_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            GuestId = _factory.GuestAId,
            CheckInDate = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CheckOutDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            AdultCount = 2,
            ChildCount = 0,
            Rooms = Array.Empty<object>()
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBookingById_AfterCreate_ReturnsBookingDetails()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create a booking first
        var createResp = await client.PostAsJsonAsync("/api/bookings", new
        {
            PropertyId = _factory.PropertyAId,
            GuestId = _factory.GuestAId,
            CheckInDate = DateTime.UtcNow.AddDays(20).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CheckOutDate = DateTime.UtcNow.AddDays(23).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            AdultCount = 1,
            ChildCount = 0,
            Rooms = new[]
            {
                new { RoomId = _factory.RoomAId, RoomTypeId = _factory.RoomTypeAId, RateApplied = 2500m }
            }
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = created.GetProperty("bookingId").GetString();

        // Now get it
        var getResp = await client.GetAsync($"/api/bookings/{bookingId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResp.Content.ReadAsStringAsync();
        body.Should().Contain("bookingReference");
    }

    [Fact]
    public async Task GetBookingById_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/bookings/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBookingsByProperty_Returns200WithPagination()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/bookings/property/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task CheckIn_ThenCheckOut_FullLifecycle()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Create booking
        var createResp = await client.PostAsJsonAsync("/api/bookings", new
        {
            PropertyId = _factory.PropertyAId,
            GuestId = _factory.GuestAId,
            CheckInDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CheckOutDate = DateTime.UtcNow.AddDays(4).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            AdultCount = 2,
            ChildCount = 0,
            Rooms = new[]
            {
                new { RoomId = _factory.RoomAId, RoomTypeId = _factory.RoomTypeAId, RateApplied = 2500m }
            }
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = created.GetProperty("bookingId").GetString();

        // Check-in
        var checkInResp = await client.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/check-in",
            new { UserId = _factory.UserAId });
        checkInResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkedIn = await checkInResp.Content.ReadAsStringAsync();
        // BookingStatus.CheckedIn serialized as string via JsonStringEnumConverter
        checkedIn.Should().Contain("\"status\":\"CheckedIn\"");

        // Check-out
        var checkOutResp = await client.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/check-out",
            new { UserId = _factory.UserAId });
        checkOutResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkedOut = await checkOutResp.Content.ReadAsStringAsync();
        // BookingStatus.CheckedOut serialized as string via JsonStringEnumConverter
        checkedOut.Should().Contain("\"status\":\"CheckedOut\"");
    }

    [Fact]
    public async Task CheckIn_NonExistentBooking_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync(
            $"/api/bookings/{Guid.NewGuid()}/check-in",
            new { UserId = _factory.UserAId });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBooking_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            PropertyId = _factory.PropertyAId,
            GuestId = _factory.GuestAId,
            CheckInDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            CheckOutDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            AdultCount = 1,
            ChildCount = 0,
            Rooms = Array.Empty<object>()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
