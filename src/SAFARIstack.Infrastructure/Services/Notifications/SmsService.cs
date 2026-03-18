using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SAFARIstack.Infrastructure.Services.Notifications;

/// <summary>
/// SMS configuration (injected from appsettings.json)
/// Supports: Twilio, AWS SNS, local HTTP gateway, or any REST-based SMS provider
/// </summary>
public class SmsConfiguration
{
    /// <summary>Provider type: "twilio", "aws-sns", "local-gateway", "generic-api"</summary>
    public string Provider { get; set; } = "local-gateway";
    
    /// <summary>API Key / Account SID for authentication</summary>
    public string ApiKey { get; set; } = "";
    
    /// <summary>API Secret / Auth Token</summary>
    public string ApiSecret { get; set; } = "";
    
    /// <summary>Sender ID / Phone number for SMS</summary>
    public string SenderId { get; set; } = "SAFARIstack";
    
    /// <summary>API endpoint for REST-based providers</summary>
    public string ApiEndpoint { get; set; } = "";
    
    /// <summary>Whether SMS sending is enabled</summary>
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// SMS request DTO
/// </summary>
public record SmsRequest(
    string PhoneNumber,
    string Message,
    string? SenderId = null,
    Dictionary<string, string>? CustomData = null);

/// <summary>
/// SMS send result
/// </summary>
public record SmsResult(
    bool IsSuccess,
    string MessageId,
    string? ErrorMessage = null,
    int? HttpStatusCode = null);

/// <summary>
/// SMS service interface
/// </summary>
public interface ISmsService
{
    Task<SmsResult> SendAsync(SmsRequest request);
    Task<SmsResult> SendBulkAsync(List<SmsRequest> requests);
    Task<bool> IsConfiguredAsync();
    Task<SmsResult> SendVerificationCodeAsync(string phoneNumber, string code);
    Task<SmsResult> SendBookingNotificationAsync(string phoneNumber, string bookingId, string propertyName, string checkInDate);
    Task<SmsResult> SendCheckInReminderAsync(string phoneNumber, string guestName, string propertyName, string checkInTime);
}

/// <summary>
/// Base SMS service implementation supporting multiple providers
/// </summary>
public class SmsService : ISmsService
{
    private readonly SmsConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration configuration, HttpClient httpClient, ILogger<SmsService> logger)
    {
        _config = configuration.GetSection("Sms").Get<SmsConfiguration>() ?? new SmsConfiguration();
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send SMS message
    /// </summary>
    public async Task<SmsResult> SendAsync(SmsRequest request)
    {
        try
        {
            if (!await IsConfiguredAsync())
            {
                _logger.LogWarning("SMS service not configured. Skipping SMS to {PhoneNumber}", request.PhoneNumber);
                return new SmsResult(false, "", "SMS service not configured");
            }

            ValidatePhoneNumber(request.PhoneNumber);

            // Route to provider-specific implementation
            return _config.Provider.ToLowerInvariant() switch
            {
                "twilio" => await SendViaTwilioAsync(request),
                "aws-sns" => await SendViaAwsSnsAsync(request),
                "local-gateway" => await SendViaLocalGatewayAsync(request),
                "generic-api" => await SendViaGenericApiAsync(request),
                _ => new SmsResult(false, "", $"Unknown SMS provider: {_config.Provider}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", request.PhoneNumber);
            return new SmsResult(false, "", ex.Message);
        }
    }

    /// <summary>
    /// Send bulk SMS messages
    /// </summary>
    public async Task<SmsResult> SendBulkAsync(List<SmsRequest> requests)
    {
        if (requests == null || requests.Count == 0)
            return new SmsResult(false, "", "No requests provided");

        try
        {
            var results = await Task.WhenAll(
                requests.Select(r => SendAsync(r))
            );

            var successCount = results.Count(r => r.IsSuccess);
            var failureCount = results.Count(r => !r.IsSuccess);

            _logger.LogInformation(
                "Bulk SMS sent: {SuccessCount} successful, {FailureCount} failed",
                successCount, failureCount);

            return new SmsResult(
                failureCount == 0,
                $"batch-{Guid.NewGuid()}",
                failureCount > 0 ? $"{failureCount} messages failed to send" : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk SMS");
            return new SmsResult(false, "", ex.Message);
        }
    }

    /// <summary>
    /// Check if SMS service is configured
    /// </summary>
    public Task<bool> IsConfiguredAsync()
    {
        var isEnabled = _config.Enabled
            && !string.IsNullOrEmpty(_config.ApiKey)
            && !string.IsNullOrEmpty(_config.ApiSecret);

        return Task.FromResult(isEnabled);
    }

    /// <summary>
    /// Send verification code via SMS
    /// </summary>
    public async Task<SmsResult> SendVerificationCodeAsync(string phoneNumber, string code)
    {
        var message = $"Your verification code is: {code}. Valid for 10 minutes. Do not share this code.";
        var request = new SmsRequest(phoneNumber, message);
        return await SendAsync(request);
    }

    /// <summary>
    /// Send booking notification SMS
    /// </summary>
    public async Task<SmsResult> SendBookingNotificationAsync(
        string phoneNumber,
        string bookingId,
        string propertyName,
        string checkInDate)
    {
        var message = $"Booking confirmed at {propertyName}. Booking ID: {bookingId}. Check-in: {checkInDate}. Reply HELP for support.";
        var request = new SmsRequest(phoneNumber, message);
        return await SendAsync(request);
    }

    /// <summary>
    /// Send check-in reminder SMS
    /// </summary>
    public async Task<SmsResult> SendCheckInReminderAsync(
        string phoneNumber,
        string guestName,
        string propertyName,
        string checkInTime)
    {
        var message = $"Hi {guestName}! Your check-in at {propertyName} is today at {checkInTime}. See you soon!";
        var request = new SmsRequest(phoneNumber, message);
        return await SendAsync(request);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PROVIDER-SPECIFIC IMPLEMENTATIONS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Send via Twilio (REST API)
    /// Requires: ApiKey = AccountSID, ApiSecret = AuthToken
    /// </summary>
    private async Task<SmsResult> SendViaTwilioAsync(SmsRequest request)
    {
        try
        {
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_config.ApiKey}/Messages.json";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("To", request.PhoneNumber),
                new KeyValuePair<string, string>("From", _config.SenderId),
                new KeyValuePair<string, string>("Body", request.Message)
            });

            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiKey}:{_config.ApiSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var messageId = doc.RootElement.GetProperty("sid").GetString() ?? Guid.NewGuid().ToString();

                _logger.LogInformation("SMS sent via Twilio to {PhoneNumber} (ID: {MessageId})", request.PhoneNumber, messageId);
                return new SmsResult(true, messageId, null, (int)response.StatusCode);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Twilio API error: {StatusCode} - {ErrorBody}", response.StatusCode, errorBody);
                return new SmsResult(false, "", errorBody, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via Twilio");
            return new SmsResult(false, "", ex.Message);
        }
    }

    /// <summary>
    /// Send via AWS SNS (REST API)
    /// </summary>
    private async Task<SmsResult> SendViaAwsSnsAsync(SmsRequest request)
    {
        // AWS SNS signing is complex - simplified version
        try
        {
            _logger.LogInformation("AWS SNS SMS not yet implemented. Message queued for {PhoneNumber}", request.PhoneNumber);
            return new SmsResult(false, "", "AWS SNS integration not yet implemented");
        }
        catch (Exception ex)
        {
            return new SmsResult(false, "", ex.Message);
        }
    }

    /// <summary>
    /// Send via local HTTP gateway (generic REST API)
    /// Expects provider to expose: POST /send with phone, message, sender parameters
    /// </summary>
    private async Task<SmsResult> SendViaLocalGatewayAsync(SmsRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ApiEndpoint))
            {
                return new SmsResult(false, "", "Local gateway endpoint not configured");
            }

            var payload = new
            {
                phone = request.PhoneNumber,
                message = request.Message,
                sender = request.SenderId ?? _config.SenderId,
                apiKey = _config.ApiKey
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add Authorization header
            content.Headers.Add("X-API-Key", _config.ApiSecret);

            var response = await _httpClient.PostAsync(_config.ApiEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var messageId = Guid.NewGuid().ToString();

                try
                {
                    var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("messageId", out var mid))
                    {
                        messageId = mid.GetString() ?? messageId;
                    }
                }
                catch { /* Use GUID if parsing fails */ }

                _logger.LogInformation("SMS sent via local gateway to {PhoneNumber} (ID: {MessageId})", request.PhoneNumber, messageId);
                return new SmsResult(true, messageId, null, (int)response.StatusCode);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Local gateway error: {StatusCode} - {ErrorBody}", response.StatusCode, errorBody);
                return new SmsResult(false, "", $"Gateway error: {response.StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via local gateway");
            return new SmsResult(false, "", ex.Message);
        }
    }

    /// <summary>
    /// Send via generic REST API
    /// Adapter for third-party SMS providers with custom endpoints
    /// </summary>
    private async Task<SmsResult> SendViaGenericApiAsync(SmsRequest request)
    {
        try
        {
            var payload = new
            {
                to = request.PhoneNumber,
                text = request.Message,
                from = request.SenderId ?? _config.SenderId
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add API key header
            content.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");

            var response = await _httpClient.PostAsync(_config.ApiEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var messageId = Guid.NewGuid().ToString();
                _logger.LogInformation("SMS sent via generic API to {PhoneNumber}", request.PhoneNumber);
                return new SmsResult(true, messageId, null, (int)response.StatusCode);
            }
            else
            {
                return new SmsResult(false, "", $"API returned {response.StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via generic API");
            return new SmsResult(false, "", ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private void ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty");

        // Remove non-digit characters for validation
        var digits = string.Concat(phoneNumber.Where(char.IsDigit));

        if (digits.Length < 7)
            throw new ArgumentException("Phone number must have at least 7 digits");

        if (digits.Length > 15)
            throw new ArgumentException("Phone number exceeds 15 digits");
    }
}
