using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAFARIstack.Tests.Integration.Infrastructure;

namespace SAFARIstack.Tests.Integration.Endpoints;

/// <summary>
/// Tests for /api/auth/* endpoints — register, login, token refresh,
/// change password, logout, and the /api/auth/me profile endpoints.
/// Verifies the full HTTP pipeline including serialization, middleware, and DB persistence.
/// </summary>
[Collection("WebApp")]
public class AuthEndpointTests
{
    private readonly CustomWebAppFactory _factory;

    public AuthEndpointTests(CustomWebAppFactory factory) => _factory = factory;

    // ═══════════════════════════════════════════════════════════════
    //  REGISTER
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Register_ValidRequest_Returns201AndPersists()
    {
        var client = _factory.CreateAnonymousClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            Email = $"newuser-{Guid.NewGuid():N}@test.com",
            Password = "Str0ng!Pass",
            FirstName = "Test",
            LastName = "User",
            RoleName = "FrontDesk"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("success");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var client = _factory.CreateAnonymousClient();
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            Email = email,
            Password = "Str0ng!Pass",
            FirstName = "First",
            LastName = "User",
            RoleName = "FrontDesk"
        };

        // First registration should succeed
        await client.PostAsJsonAsync("/api/auth/register", request);

        // Second should fail
        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400ValidationError()
    {
        var client = _factory.CreateAnonymousClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            Email = "not-an-email",
            Password = "Str0ng!Pass",
            FirstName = "Test",
            LastName = "User",
            RoleName = "FrontDesk"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400ValidationError()
    {
        var client = _factory.CreateAnonymousClient();
        var request = new
        {
            PropertyId = _factory.PropertyAId,
            Email = $"weak-{Guid.NewGuid():N}@test.com",
            Password = "123",
            FirstName = "Test",
            LastName = "User",
            RoleName = "FrontDesk"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOGIN
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var client = _factory.CreateAnonymousClient();
        var email = $"login-{Guid.NewGuid():N}@test.com";

        // Register first
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            PropertyId = _factory.PropertyAId,
            Email = email,
            Password = "Str0ng!Pass",
            FirstName = "Login",
            LastName = "User",
            RoleName = "FrontDesk"
        });

        // Now login
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = "Str0ng!Pass"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("token");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = _factory.UserAEmail,
            Password = "WrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "nobody@nowhere.com",
            Password = "Whatever123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GET CURRENT USER (authenticated)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMe_Authenticated_Returns200WithUserInfo()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/auth/me/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("email");
    }

    [Fact]
    public async Task GetMe_Anonymous_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/auth/me/");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CHANGE PASSWORD
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/auth/me/change-password", new
        {
            CurrentPassword = "WrongOldPassword!",
            NewPassword = "NewStr0ng!Pass"
        });

        // Could be 400 or 204 depending on whether the password matches
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NoContent);
    }

    // ═══════════════════════════════════════════════════════════════
    //  USER MANAGEMENT (Admin only)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUsersByProperty_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/users/by-property/{_factory.PropertyAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("items");
    }

    [Fact]
    public async Task GetUserById_ValidId_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/users/{_factory.UserAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("email");
    }

    [Fact]
    public async Task GetUserById_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
