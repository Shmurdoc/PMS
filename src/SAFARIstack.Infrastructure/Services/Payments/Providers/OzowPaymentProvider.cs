using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Services;

namespace SAFARIstack.Infrastructure.Services.Payments.Providers;

/// <summary>
/// Ozow payment gateway integration
/// South African payment provider supporting cards, EFT, and mobile payments
/// https://www.ozow.com
/// </summary>
public class OzowPaymentProvider : IPaymentGatewayProvider
{
    public string ProviderName => "Ozow";

    private readonly HttpClient _httpClient;
    private readonly OzowConfiguration _config;
    private readonly ILogger<OzowPaymentProvider> _logger;

    private const string SandboxBaseUrl = "https://api.sandbox.ozow.com";
    private const string LiveBaseUrl = "https://api.ozow.com";

    public OzowPaymentProvider(
        HttpClient httpClient,
        OzowConfiguration config,
        ILogger<OzowPaymentProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentAuthorizationResult> AuthorizeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Ozow authorize: Amount={Amount} {Currency} Description={Description}",
            amount, currency, description);

        var externalId = Guid.NewGuid().ToString("N")[..16].ToUpper();
        return new PaymentAuthorizationResult(
            ExternalAuthorizationId: externalId,
            Status: "pending",
            FailureReason: null,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            CardLast4: null,
            CardBrand: null);
    }

    public async Task<PaymentCaptureResult> CaptureAsync(
        string externalAuthorizationId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Ozow does not support pre-authorization capture");
        throw new NotSupportedException("Ozow uses hosted checkout; use ChargeAsync instead");
    }

    public async Task<PaymentCaptureResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _config.IsLive ? LiveBaseUrl : SandboxBaseUrl;

        var payload = new OzowPaymentRequest
        {
            Amount = (long)(amount * 100), // Convert to cents
            CurrencyCode = currency,
            Reference = idempotencyKey,
            IsTest = !_config.IsLive
        };

        try
        {
            var content = JsonContent.Create(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/payment/create")
            {
                Content = content
            };

            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Ozow charge response: Status={Status} Content={Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentCaptureResult(
                    ExternalChargeId: Guid.NewGuid().ToString("N")[..12],
                    Status: "failed",
                    FailureReason: $"HTTP {response.StatusCode}: {responseContent}",
                    ReceiptUrl: null,
                    ReceiptNumber: null);
            }

            var ozowResponse = JsonSerializer.Deserialize<OzowPaymentResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentCaptureResult(
                ExternalChargeId: ozowResponse?.RequestId ?? Guid.NewGuid().ToString("N")[..12],
                Status: "pending",
                FailureReason: null,
                ReceiptUrl: ozowResponse?.CheckoutUrl,
                ReceiptNumber: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozow charge error");
            throw;
        }
    }

    public async Task<PaymentRefundResult> RefundAsync(
        string externalChargeId,
        decimal amount,
        string reason = "customer_request",
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _config.IsLive ? LiveBaseUrl : SandboxBaseUrl;

        try
        {
            var refundAmount = amount * 100; // Convert to cents

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{baseUrl}/api/payment/refund")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("request_id", externalChargeId),
                    new KeyValuePair<string, string>("amount", ((long)refundAmount).ToString()),
                    new KeyValuePair<string, string>("reason", reason)
                })
            };

            request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Ozow refund response: Status={Status} Content={Content}",
                response.StatusCode, content);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentRefundResult(
                    ExternalRefundId: Guid.NewGuid().ToString("N")[..12],
                    Status: "failed",
                    FailureReason: $"HTTP {response.StatusCode}",
                    ReceiptUrl: null);
            }

            // Parse the refund response
            var refundResponse = JsonSerializer.Deserialize<OzowRefundResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentRefundResult(
                ExternalRefundId: refundResponse?.RefundId ?? Guid.NewGuid().ToString("N")[..12],
                Status: "refunded",
                FailureReason: null,
                ReceiptUrl: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozow refund error");
            throw;
        }
    }

    public async Task<PaymentVoidResult> VoidAsync(
        string externalAuthorizationId,
        string reason = "merchant_cancel",
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Ozow void not applicable (no pre-auth)");
        return new PaymentVoidResult(ExternalVoidId: "na", Status: "na", FailureReason: null);
    }

    public async Task<PaymentStatus> GetTransactionStatusAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _config.IsLive ? LiveBaseUrl : SandboxBaseUrl;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{baseUrl}/api/payment/status/{externalTransactionId}");
            request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP {response.StatusCode}: {content}");
            }

            var status = JsonSerializer.Deserialize<OzowStatusResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentStatus(
                ExternalTransactionId: externalTransactionId,
                State: status?.Status?.ToLower() ?? "unknown",
                Amount: (status?.Amount ?? 0) / 100m, // Convert from cents
                Currency: status?.CurrencyCode ?? "ZAR",
                CapturedAt: DateTime.UtcNow,
                RefundedAt: null,
                VoidedAt: null,
                LastMessage: status?.Status,
                RetrievedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Ozow transaction status");
            throw;
        }
    }

    public async Task<List<PaymentTransaction>> ListTransactionsAsync(
        Guid? propertyId,
        DateTime fromDate,
        DateTime toDate,
        List<string>? statuses = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Ozow transaction listing not yet implemented");
        return new List<PaymentTransaction>();
    }

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        // Ozow webhook verification
        var hash = ComputeHmacSha512(payload, secret);
        return hash.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        return VerifyWebhookSignature(payload, signature, _config.WebhookSecret ?? "");
    }

    public async Task<List<PaymentMethodInfo>> GetSupportedMethodsAsync(
        CancellationToken cancellationToken = default)
    {
        return new List<PaymentMethodInfo>
        {
            new("card", "Credit/Debit Card", 2.50m, 1, 100000, new List<string> { "ZAR" }, true),
            new("eft", "Electronic Funds Transfer", 2.00m, 5, 100000, new List<string> { "ZAR" }, true),
            new("mobile_wallet", "Mobile Wallet", 2.75m, 1, 100000, new List<string> { "ZAR" }, true)
        };
    }

    private static string MapOzowStatusToStandard(string? ozowStatus) => ozowStatus?.ToLower() switch
    {
        "complete" => "captured",
        "pending" => "authorized",
        "failed" => "failed",
        "cancelled" => "failed",
        _ => "unknown"
    };

    private static string ComputeHmacSha512(string input, string secret)
    {
        using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}

// ─── Configuration ─────────────────────────────────────────────────────

public class OzowConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string? MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public bool IsLive { get; set; }
}

// ─── API Models ─────────────────────────────────────────────────────

public class OzowPaymentRequest
{
    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = "ZAR";

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";

    [JsonPropertyName("isTest")]
    public bool IsTest { get; set; }
}

public class OzowPaymentResponse
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = "";

    [JsonPropertyName("checkoutUrl")]
    public string CheckoutUrl { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}

public class OzowStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = "ZAR";

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";
}

public class OzowWebhookPayload
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = "ZAR";

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";
}

public class OzowRefundResponse
{
    [JsonPropertyName("refundId")]
    public string RefundId { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }
}
