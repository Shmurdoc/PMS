using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SAFARIstack.Infrastructure.Authentication;

/// <summary>
/// Custom authentication handler for RFID readers using X-Reader-API-Key header
/// </summary>
public class RfidReaderAuthenticationHandler : AuthenticationHandler<RfidReaderAuthenticationOptions>
{
    private readonly RfidAuthenticationSettings _settings;

    public RfidReaderAuthenticationHandler(
        IOptionsMonitor<RfidReaderAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        RfidAuthenticationSettings settings)
        : base(options, logger, encoder)
    {
        _settings = settings;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Reader-API-Key", out var apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail("Missing X-Reader-API-Key header");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(providedApiKey))
        {
            return AuthenticateResult.Fail("Invalid X-Reader-API-Key header");
        }

        // IP whitelist check (if enabled)
        if (_settings.EnableIpWhitelist)
        {
            var remoteIp = Context.Connection.RemoteIpAddress?.ToString();
            if (remoteIp == null || !_settings.AllowedIpAddresses.Contains(remoteIp))
            {
                Logger.LogWarning("RFID reader authentication failed: IP {IpAddress} not in whitelist", remoteIp);
                return AuthenticateResult.Fail("IP address not authorized");
            }
        }

        // API key is validated in the command handler against the database
        // Here we just verify the format and create a basic principal
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "RFID-Reader"),
            new Claim(ClaimTypes.Role, "RfidReader"),
            new Claim("ApiKey", providedApiKey)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

public class RfidReaderAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "RfidReader";
}
