using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Services;

namespace SAFARIstack.Infrastructure.Services.Payments.Providers;

/// <summary>
/// PayFast payment gateway integration
/// South African payment provider supporting cards, EFT, and PayPal
/// https://www.payfast.co.za
/// </summary>
public class PayFastPaymentProvider : IPaymentGatewayProvider
{
    public string ProviderName => "PayFast";

    private readonly HttpClient _httpClient;
    private readonly PayFastConfiguration _config;
    private readonly ILogger<PayFastPaymentProvider> _logger;

    private const string SandboxBaseUrl = "https://sandbox.payfast.co.za";
    private const string LiveBaseUrl = "https://www.payfast.co.za";

    public PayFastPaymentProvider(
        HttpClient httpClient,
        PayFastConfiguration config,
        ILogger<PayFastPaymentProvider> logger)
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
        // PayFast doesn't support pre-authorization in standard mode
        // Generate a transaction reference for tracking
        _logger.LogInformation(
            "PayFast authorize: Amount={Amount} {Currency} Description={Description}",
            amount, currency, description);

        var externalId = Guid.NewGuid().ToString("N")[..16].ToUpper();
        return new PaymentAuthorizationResult(
            ExternalAuthorizationId: externalId,
            Status: "pending_redirect",
            FailureReason: null,
            ExpiresAt: DateTime.UtcNow.AddMinutes(30),
            CardLast4: null,
            CardBrand: null);
    }

    public async Task<PaymentCaptureResult> CaptureAsync(
        string externalAuthorizationId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("PayFast does not support capture; use ChargeAsync instead");
        throw new NotSupportedException("PayFast requires direct charge, not pre-authorized capture");
    }

    public async Task<PaymentCaptureResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // PayFast uses hosted payment page approach
        // Create payment on PayFast and return the redirect URL
        var baseUrl = _config.IsLive ? LiveBaseUrl : SandboxBaseUrl;

        var payload = new PayFastPaymentRequest
        {
            merchant_id = _config.MerchantId,
            merchant_key = _config.MerchantKey,
            return_url = _config.ReturnUrl,
            cancel_url = _config.CancelUrl,
            notify_url = _config.NotifyUrl,
            name_first = _config.CustomerFirstName ?? "Guest",
            name_last = _config.CustomerLastName ?? "User",
            email_address = _config.CustomerEmail ?? "guest@safari.local",
            cell_number = _config.CustomerPhone,
            m_payment_id = idempotencyKey,
            amount = amount.ToString("F2"),
            item_name = description,
            item_description = description,
            custom_int1 = 0, // For internal use
            custom_str1 = "", // For internal use
            payment_method = "" // Let customer choose
        };

        // Calculate signature
        payload.signature = GeneratePayFastSignature(payload, _config.PassPhrase);

        try
        {
            // PayFast uses POST for hosted checkout
            var content = new FormUrlEncodedContent(ConvertToFormData(payload));

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{baseUrl}/eng/process")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);

            // Note: PayFast redirects, doesn't return JSON
            // In production, the frontend handles the redirect
            var externalId = payload.m_payment_id;

            _logger.LogInformation(
                "PayFast charge initiated: PaymentId={PaymentId} Amount={Amount}",
                externalId, amount);

            return new PaymentCaptureResult(
                ExternalChargeId: externalId,
                Status: "pending",
                FailureReason: null,
                ReceiptUrl: $"{baseUrl}/eng/process",
                ReceiptNumber: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayFast charge error");
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
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{baseUrl}/eng/utils/api/refund")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("merchant_id", _config.MerchantId),
                    new KeyValuePair<string, string>("merchant_key", _config.MerchantKey),
                    new KeyValuePair<string, string>("transaction_id", externalChargeId),
                    new KeyValuePair<string, string>("amount", amount.ToString("F2")),
                })
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "PayFast refund response: Status={Status} Content={Content}",
                response.StatusCode, content);

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentRefundResult(
                    ExternalRefundId: Guid.NewGuid().ToString("N")[..12],
                    Status: "failed",
                    FailureReason: $"HTTP {response.StatusCode}",
                    ReceiptUrl: null);
            }

            return new PaymentRefundResult(
                ExternalRefundId: Guid.NewGuid().ToString("N")[..12],
                Status: "refunded",
                FailureReason: null,
                ReceiptUrl: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayFast refund error");
            throw;
        }
    }

    public async Task<PaymentVoidResult> VoidAsync(
        string externalAuthorizationId,
        string reason = "merchant_cancel",
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("PayFast void not applicable");
        return new PaymentVoidResult(ExternalVoidId: "na", Status: "na", FailureReason: null);
    }

    public async Task<PaymentStatus> GetTransactionStatusAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _config.IsLive ? LiveBaseUrl : SandboxBaseUrl;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{baseUrl}/eng/utils/api/query")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("merchant_id", _config.MerchantId),
                    new KeyValuePair<string, string>("merchant_key", _config.MerchantKey),
                    new KeyValuePair<string, string>("transaction_id", externalTransactionId),
                })
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP {response.StatusCode}: {content}");
            }

            // Parse response (typically space-separated values)
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var status = lines.FirstOrDefault()?.Trim().ToLower() ?? "unknown";

            return new PaymentStatus(
                ExternalTransactionId: externalTransactionId,
                State: MapPayFastStatusToStandard(status),
                Amount: 0, // PayFast API may not return amount
                Currency: "ZAR",
                CapturedAt: DateTime.UtcNow,
                RefundedAt: null,
                VoidedAt: null,
                LastMessage: status,
                RetrievedAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying PayFast transaction status");
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
        _logger.LogWarning("PayFast transaction listing not yet implemented");
        return new List<PaymentTransaction>();
    }

    public bool VerifyWebhookSignature(string payload, string signature, string secret)
    {
        // PayFast webhook verification
        // Parse the posted array data into a string
        var dataString = payload.Replace("&", "&");
        if (!string.IsNullOrEmpty(secret))
        {
            dataString += $"&passphrase={Uri.EscapeDataString(secret)}";
        }

        var hash = ComputeMd5(dataString);
        return hash.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        return VerifyWebhookSignature(payload, signature, _config.PassPhrase ?? "");
    }

    public async Task<List<PaymentMethodInfo>> GetSupportedMethodsAsync(
        CancellationToken cancellationToken = default)
    {
        return new List<PaymentMethodInfo>
        {
            new("credit_card", "Credit/Debit Card", 2.99m, 1, 100000, new List<string> { "ZAR" }, true),
            new("eft", "Electronic Funds Transfer", 0.99m, 5, 100000, new List<string> { "ZAR" }, true),
            new("paypal", "PayPal", 3.50m, 1, 100000, new List<string> { "ZAR" }, true)
        };
    }

    private static string MapPayFastStatusToStandard(string? payFastStatus) => payFastStatus?.ToLower() switch
    {
        "complete" => "captured",
        "pending" => "authorized",
        "failed" => "failed",
        "cancelled" => "failed",
        _ => "unknown"
    };

    private string GeneratePayFastSignature(PayFastPaymentRequest data, string? passPhrase)
    {
        var fields = new StringBuilder();
        fields.Append($"merchant_id={Uri.EscapeDataString(data.merchant_id)}");
        fields.Append($"&merchant_key={Uri.EscapeDataString(data.merchant_key)}");
        fields.Append($"&return_url={Uri.EscapeDataString(data.return_url ?? "")}");
        fields.Append($"&cancel_url={Uri.EscapeDataString(data.cancel_url ?? "")}");
        fields.Append($"&notify_url={Uri.EscapeDataString(data.notify_url ?? "")}");
        fields.Append($"&name_first={Uri.EscapeDataString(data.name_first)}");
        fields.Append($"&name_last={Uri.EscapeDataString(data.name_last)}");
        fields.Append($"&email_address={Uri.EscapeDataString(data.email_address)}");
        fields.Append($"&m_payment_id={Uri.EscapeDataString(data.m_payment_id)}");
        fields.Append($"&amount={Uri.EscapeDataString(data.amount)}");
        fields.Append($"&item_name={Uri.EscapeDataString(data.item_name)}");
        fields.Append($"&item_description={Uri.EscapeDataString(data.item_description)}");

        if (!string.IsNullOrEmpty(data.cell_number))
            fields.Append($"&cell_number={Uri.EscapeDataString(data.cell_number)}");

        if (!string.IsNullOrEmpty(passPhrase))
            fields.Append($"&passphrase={Uri.EscapeDataString(passPhrase)}");

        return ComputeMd5(fields.ToString());
    }

    private static string ComputeMd5(string input)
    {
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    private static Dictionary<string, string> ConvertToFormData(PayFastPaymentRequest data)
    {
        var dict = new Dictionary<string, string>
        {
            { "merchant_id", data.merchant_id },
            { "merchant_key", data.merchant_key },
            { "return_url", data.return_url ?? "" },
            { "cancel_url", data.cancel_url ?? "" },
            { "notify_url", data.notify_url ?? "" },
            { "name_first", data.name_first },
            { "name_last", data.name_last },
            { "email_address", data.email_address },
            { "m_payment_id", data.m_payment_id },
            { "amount", data.amount },
            { "item_name", data.item_name },
            { "item_description", data.item_description },
            { "signature", data.signature }
        };

        if (!string.IsNullOrEmpty(data.cell_number))
            dict["cell_number"] = data.cell_number;

        return dict;
    }

    private static Dictionary<string, string> ParsePayFastWebhook(string payload)
    {
        var dict = new Dictionary<string, string>();
        var pairs = payload.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
                dict[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
        }
        return dict;
    }
}

// ─── Configuration ─────────────────────────────────────────────────────

public class PayFastConfiguration
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string? PassPhrase { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? NotifyUrl { get; set; }
    public bool IsLive { get; set; }
    public string? CustomerFirstName { get; set; }
    public string? CustomerLastName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
}

// ─── API Models ─────────────────────────────────────────────────────

public class PayFastPaymentRequest
{
    public string merchant_id { get; set; } = string.Empty;
    public string merchant_key { get; set; } = string.Empty;
    public string? return_url { get; set; }
    public string? cancel_url { get; set; }
    public string? notify_url { get; set; }
    public string name_first { get; set; } = string.Empty;
    public string name_last { get; set; } = string.Empty;
    public string email_address { get; set; } = string.Empty;
    public string? cell_number { get; set; }
    public string m_payment_id { get; set; } = string.Empty;
    public string amount { get; set; } = string.Empty;
    public string item_name { get; set; } = string.Empty;
    public string item_description { get; set; } = string.Empty;
    public int custom_int1 { get; set; }
    public string custom_str1 { get; set; } = string.Empty;
    public string payment_method { get; set; } = string.Empty;
    public string signature { get; set; } = string.Empty;
}
