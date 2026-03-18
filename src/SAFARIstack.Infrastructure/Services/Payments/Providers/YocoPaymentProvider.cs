using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Services;

namespace SAFARIstack.Infrastructure.Services.Payments.Providers;

/// <summary>
/// Yoco payment gateway integration
/// South African payment provider supporting cards, EFT, and mobile payments
/// https://www.yoco.com
/// </summary>
public class YocoPaymentProvider : IPaymentGatewayProvider
{
    public string ProviderName => "Yoco";

    private readonly HttpClient _httpClient;
    private readonly YocoConfiguration _config;
    private readonly ILogger<YocoPaymentProvider> _logger;

    private const string SandboxBaseUrl = "https://staging-api.yoco.com";
    private const string LiveBaseUrl = "https://api.yoco.com";

    public YocoPaymentProvider(
        HttpClient httpClient,
        YocoConfiguration config,
        ILogger<YocoPaymentProvider> logger)
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
            "Yoco authorize: Amount={Amount} {Currency} Description={Description}",
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
        _logger.LogWarning("Yoco does not support pre-authorization capture");
        throw new NotSupportedException("Yoco uses hosted checkout; use ChargeAsync instead");
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

        var payload = new YocoPaymentRequest
        {
            Amount = (long)(amount * 100), // Convert to cents
            Currency = currency,
            IdempotencyKey = idempotencyKey,
            Description = description,
            Metadata = new Dictionary<string, string>
            {
                { "reference", idempotencyKey },
                { "platform", "safaristack" }
            }
        };

        try
        {
            var content = JsonContent.Create(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/charges")
            {
                Content = content
            };

            // Add authentication header
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiKey}:"));
            request.Headers.Add("Authorization", $"Basic {authHeader}");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Yoco charge response: Status={Status} Content={Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentCaptureResult(
                    ExternalChargeId: Guid.NewGuid().ToString("N")[..12],
                    Status: "failed",
                    FailureReason: $"HTTP {response.StatusCode}: {responseContent}",
                    ReceiptUrl: "",
                    ReceiptNumber: null);
            }

            var yocoResponse = JsonSerializer.Deserialize<YocoChargeResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentCaptureResult(
                ExternalChargeId: yocoResponse?.Id ?? Guid.NewGuid().ToString("N")[..12],
                Status: yocoResponse?.Status?.ToLower() ?? "pending",
                FailureReason: yocoResponse?.FailureMessage,
                ReceiptUrl: yocoResponse?.ReceiptUrl ?? "",
                ReceiptNumber: yocoResponse?.Reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yoco charge error");
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

        var payload = new YocoRefundRequest
        {
            Amount = (long)(amount * 100), // Convert to cents
            Reason = reason
        };

        try
        {
            var content = JsonContent.Create(payload);
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{baseUrl}/v1/charges/{externalChargeId}/refunds")
            {
                Content = content
            };

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiKey}:"));
            request.Headers.Add("Authorization", $"Basic {authHeader}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Yoco refund response: Status={Status} Content={Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentRefundResult(
                    ExternalRefundId: Guid.NewGuid().ToString("N")[..12],
                    Status: "failed",
                    FailureReason: $"HTTP {response.StatusCode}",
                    ReceiptUrl: null);
            }

            var refundResponse = JsonSerializer.Deserialize<YocoRefundResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentRefundResult(
                ExternalRefundId: refundResponse?.Id ?? Guid.NewGuid().ToString("N")[..12],
                Status: "refunded",
                FailureReason: null,
                ReceiptUrl: refundResponse?.ReceiptUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yoco refund error");
            throw;
        }
    }

    public async Task<PaymentVoidResult> VoidAsync(
        string externalAuthorizationId,
        string reason = "merchant_cancel",
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Yoco void not applicable (no pre-auth)");
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
                $"{baseUrl}/v1/charges/{externalTransactionId}");

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiKey}:"));
            request.Headers.Add("Authorization", $"Basic {authHeader}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP {response.StatusCode}: {content}");
            }

            var charge = JsonSerializer.Deserialize<YocoChargeResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentStatus(
                ExternalTransactionId: externalTransactionId,
                State: charge?.Status?.ToLower() ?? "unknown",
                Amount: (charge?.Amount ?? 0) / 100m, // Convert from cents
                Currency: charge?.Currency ?? "ZAR",
                CapturedAt: charge?.CreatedDate ?? DateTime.UtcNow,
                RefundedAt: charge?.RefundedDate,
                VoidedAt: null,
                LastMessage: charge?.Status,
                RetrievedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Yoco transaction status");
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
        var baseUrl = _config.IsLive ? LiveBaseUrl : SandboxBaseUrl;

        try
        {
            // Build query parameters
            var queryParams = new List<string>
            {
                $"from={fromDate:O}",
                $"to={toDate:O}"
            };

            if (statuses?.Any() == true)
            {
                queryParams.AddRange(statuses.Select(s => $"status={Uri.EscapeDataString(s)}"));
            }

            var queryString = string.Join("&", queryParams);
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{baseUrl}/v1/charges?{queryString}");

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ApiKey}:"));
            request.Headers.Add("Authorization", $"Basic {authHeader}");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Yoco list transactions failed: Status={Status} Content={Content}",
                    response.StatusCode, content);
                return new List<PaymentTransaction>();
            }

            var listResponse = JsonSerializer.Deserialize<YocoListResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var transactions = listResponse?.Data?.Select(c => new PaymentTransaction(
                ExternalId: c.Id,
                Type: "charge",
                Amount: c.Amount / 100m,
                Currency: c.Currency,
                Status: c.Status,
                PaymentMethod: "card",
                CardLast4: null,
                CardBrand: null,
                TransactionDate: c.CreatedDate,
                Description: c.Reference
            )).ToList() ?? new List<PaymentTransaction>();

            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Yoco transactions");
            return new List<PaymentTransaction>();
        }
    }

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        // Yoco webhook verification using HMAC-SHA256
        var hash = ComputeHmacSha256(payload, secret);
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
            new("card", "Credit/Debit Card", 2.99m, 1, 100000, new List<string> { "ZAR" }, true),
            new("eft", "Electronic Funds Transfer", 0.99m, 5, 100000, new List<string> { "ZAR" }, true),
            new("scan_to_pay", "Scan to Pay (Till)", 1.99m, 1, 50000, new List<string> { "ZAR" }, true)
        };
    }

    private static string ComputeHmacSha256(string input, string secret)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}

// ─── Configuration ─────────────────────────────────────────────────────

public class YocoConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsLive { get; set; }
}

// ─── API Models ─────────────────────────────────────────────────────

public class YocoPaymentRequest
{
    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "ZAR";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("idempotencyKey")]
    public string IdempotencyKey { get; set; } = "";

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

public class YocoChargeResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "ZAR";

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("refundedDate")]
    public DateTime? RefundedDate { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = "";

    [JsonPropertyName("receiptUrl")]
    public string ReceiptUrl { get; set; } = "";

    [JsonPropertyName("failureMessage")]
    public string? FailureMessage { get; set; }
}

public class YocoRefundRequest
{
    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "customer_request";
}

public class YocoRefundResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("receiptUrl")]
    public string ReceiptUrl { get; set; } = "";
}

public class YocoListResponse
{
    [JsonPropertyName("data")]
    public List<YocoChargeResponse>? Data { get; set; }
}

public class YocoWebhookPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = ""; // "charge.completed", "charge.failed", "charge.refunded"

    [JsonPropertyName("data")]
    public YocoChargeResponse? Data { get; set; }
}
