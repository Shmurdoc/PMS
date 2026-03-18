using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SAFARIstack.Tests.Integration.Infrastructure;

/// <summary>
/// Fake authentication handler for endpoint tests. Reads claim values from
/// custom request headers (X-Test-UserId, X-Test-PropertyId, X-Test-Role, X-Test-Email).
/// If no X-Test-UserId header is present, authentication fails (simulating anonymous).
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no test user header, return NoResult (anonymous)
        if (!Request.Headers.TryGetValue("X-Test-UserId", out var userIdHeader) ||
            string.IsNullOrEmpty(userIdHeader.FirstOrDefault()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdHeader.First()!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
        };

        if (Request.Headers.TryGetValue("X-Test-PropertyId", out var propId))
            claims.Add(new Claim("propertyId", propId.First()!));

        if (Request.Headers.TryGetValue("X-Test-Email", out var email))
            claims.Add(new Claim(ClaimTypes.Email, email.First()!));

        if (Request.Headers.TryGetValue("X-Test-Role", out var roles))
        {
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role!));
        }

        // Add all permissions for SuperAdmin
        foreach (var (permName, _) in SAFARIstack.Core.Domain.Entities.Permissions.GetAll())
            claims.Add(new Claim("permission", permName));

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
