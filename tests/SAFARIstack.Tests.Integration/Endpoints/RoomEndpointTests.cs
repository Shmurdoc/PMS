using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/rooms/* endpoints — CRUD, status, floor, room types, blocks.
/// </summary>
[Collection("WebApp")]
public class RoomEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public RoomEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateRoom_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            PropertyId = _factory.PropertyAId,
            RoomTypeId = _factory.RoomTypeAId,
            RoomNumber = $"R-{Guid.NewGuid():N}"[..10],
            Floor = 2,
            Wing = "B"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("id");
    }

    [Fact]
    public async Task GetAvailableRooms_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var checkIn = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var checkOut = DateTime.UtcNow.AddDays(13).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await client.GetAsync(
            $"/api/rooms/available/{_factory.PropertyAId}?checkIn={checkIn}&checkOut={checkOut}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task GetRoomsByFloor_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/rooms/floor/{_factory.PropertyAId}/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRoomTypes_Returns200WithData()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/rooms/types/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Deluxe Suite");
    }

    [Fact]
    public async Task MarkRoomDirty_ThenClean_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient();

        // Mark dirty
        var dirtyResponse = await client.PostAsync($"/api/rooms/{_factory.RoomAId}/dirty", null);
        dirtyResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Mark clean
        var cleanResponse = await client.PostAsync($"/api/rooms/{_factory.RoomAId}/clean", null);
        cleanResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkRoom_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsync($"/api/rooms/{Guid.NewGuid()}/dirty", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRoom_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            PropertyId = _factory.PropertyAId,
            RoomTypeId = _factory.RoomTypeAId,
            RoomNumber = "ANON-01"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateRoomBlock_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/rooms/blocks", new
        {
            PropertyId = _factory.PropertyAId,
            RoomId = _factory.RoomAId,
            StartDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            EndDate = DateTime.UtcNow.AddDays(35).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Reason = 0, // Maintenance
            Notes = "Renovation"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
