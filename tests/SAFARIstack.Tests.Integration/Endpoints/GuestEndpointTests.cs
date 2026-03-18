using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/guests/* endpoints — CRUD, search, blacklist, returning guests.
/// Verifies data round-trips through HTTP → endpoint → domain → EF Core → PostgreSQL.
/// </summary>
[Collection("WebApp")]
public class GuestEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public GuestEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateGuest_ValidRequest_Returns201AndPersists()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = $"jane-{Guid.NewGuid():N}@test.com",
            Phone = "+27821111111"
        };

        var response = await client.PostAsJsonAsync("/api/guests", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("id");
        body.Should().Contain("fullName");
    }

    [Fact]
    public async Task CreateGuest_MissingFirstName_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            FirstName = "",
            LastName = "Doe"
        };

        var response = await client.PostAsJsonAsync("/api/guests", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGuestById_Exists_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/guests/{_factory.GuestAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("John");
    }

    [Fact]
    public async Task GetGuestById_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/guests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchGuests_ValidQuery_Returns200WithPagination()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/guests/search/{_factory.PropertyAId}?q=John");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task UpdateGuestContact_ValidRequest_Returns204()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync(
            $"/api/guests/{_factory.GuestAId}/contact",
            new { Email = "updated@test.com", Phone = "+27829999999" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task BlacklistGuest_ValidReason_Returns204()
    {
        // Create a new guest to blacklist (don't blacklist the shared fixture guest)
        var client = _factory.CreateAuthenticatedClient();
        var createResp = await client.PostAsJsonAsync("/api/guests", new
        {
            PropertyId = _factory.PropertyAId,
            FirstName = "Bad",
            LastName = "Guest",
            Email = $"bad-{Guid.NewGuid():N}@test.com"
        });
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var guestId = created!["id"].ToString();

        var response = await client.PostAsJsonAsync(
            $"/api/guests/{guestId}/blacklist",
            new { Reason = "Repeated violations of property rules" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetReturningGuests_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/guests/{_factory.PropertyAId}/returning");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task CreateGuest_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/guests", new
        {
            PropertyId = _factory.PropertyAId,
            FirstName = "Anon",
            LastName = "Guest"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
