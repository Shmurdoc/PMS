namespace SAFARIstack.Infrastructure.Authentication;

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

/// <summary>
/// RFID Reader authentication settings
/// </summary>
public class RfidAuthenticationSettings
{
    public bool EnableApiKeyAuth { get; set; } = true;
    public bool EnableIpWhitelist { get; set; } = false;
    public List<string> AllowedIpAddresses { get; set; } = new();
    public int VelocityCheckSeconds { get; set; } = 5;
}
