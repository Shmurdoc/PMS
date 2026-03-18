namespace SAFARIstack.Core.Domain.Services;

/// <summary>
/// Abstraction for payment gateway integrations (Stripe, Square, PayFast, etc.)
/// Provides unified interface for authorization, capture, refunds, and reconciliation
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Authorize a payment without capturing (pre-authorization)
    /// </summary>
    /// <param name="propertyId">Property initiating payment</param>
    /// <param name="amount">Amount in cents/smallest currency unit</param>
    /// <param name="currency">ISO currency code (ZAR, USD, etc.)</param>
    /// <param name="paymentMethodId">Payment token from frontend</param>
    /// <param name="description">Payment description for statement</param>
    /// <param name="idempotencyKey">Unique key for idempotency (prevent duplicate charges)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization with external transaction ID</returns>
    Task<PaymentAuthorization> AuthorizeAsync(
        Guid propertyId,
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Capture a previously authorized payment
    /// </summary>
    /// <param name="authorizationId">Authorization ID from AuthorizeAsync</param>
    /// <param name="amount">Amount to capture (null = full authorization)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Charge with external ID and receipt details</returns>
    Task<PaymentCapture> CaptureAsync(
        Guid authorizationId,
        decimal? amount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Charge payment immediately (authorize + capture in one operation)
    /// </summary>
    /// <param name="propertyId">Property initiating payment</param>
    /// <param name="amount">Amount in cents/smallest currency unit</param>
    /// <param name="currency">ISO currency code</param>
    /// <param name="paymentMethodId">Payment token</param>
    /// <param name="description">Payment description</param>
    /// <param name="idempotencyKey">Unique key for idempotency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Charge with external ID and receipt</returns>
    Task<PaymentCapture> ChargeAsync(
        Guid propertyId,
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund a captured payment (full or partial)
    /// </summary>
    /// <param name="chargeId">Charge ID to refund</param>
    /// <param name="amount">Amount to refund (null = full refund)</param>
    /// <param name="reason">Refund reason (customer_request, fraud, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund with external ID and status</returns>
    Task<PaymentRefund> RefundAsync(
        Guid chargeId,
        decimal? amount = null,
        string reason = "customer_request",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Void a pre-authorization (release hold on funds)
    /// </summary>
    /// <param name="authorizationId">Authorization ID to void</param>
    /// <param name="reason">Void reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Void confirmation</returns>
    Task<PaymentVoid> VoidAsync(
        Guid authorizationId,
        string reason = "merchant_cancel",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment status from external gateway
    /// </summary>
    /// <param name="externalTransactionId">Transaction ID from gateway (Stripe charge ID, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current payment status</returns>
    Task<PaymentStatus> GetTransactionStatusAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List transactions for reconciliation (date range, property)
    /// </summary>
    /// <param name="propertyId">Filter by property (null = all properties)</param>
    /// <param name="fromDate">Start date (UTC)</param>
    /// <param name="toDate">End date (UTC)</param>
    /// <param name="statuses">Filter by status (null = all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions for reconciliation</returns>
    Task<List<PaymentTransaction>> ListTransactionsAsync(
        Guid? propertyId,
        DateTime fromDate,
        DateTime toDate,
        List<string>? statuses = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify webhook signature from payment gateway (prevents spoofing)
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">Gateway signature header</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supported payment methods and their fee percentages
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supported methods with fees</returns>
    Task<List<PaymentMethodInfo>> GetSupportedMethodsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Authorization response from payment gateway
/// </summary>
public record PaymentAuthorization(
    Guid Id,
    string ExternalAuthorizationId,
    Guid PropertyId,
    decimal Amount,
    string Currency,
    string Status,                      // "authorized", "failed", "declined"
    string? FailureReason,              // If declined
    DateTime ExpiresAt,                 // Authorization hold expiration
    string PaymentMethod,               // "card", "apple_pay", "google_pay", etc.
    string? CardLast4,                  // Last 4 digits if card
    string? CardBrand,                  // "visa", "mastercard", etc.
    DateTime CreatedAt);

/// <summary>
/// Capture response from payment gateway
/// </summary>
public record PaymentCapture(
    Guid Id,
    string ExternalChargeId,
    Guid PropertyId,
    Guid? AuthorizationId,              // If captured from pre-auth
    decimal Amount,
    string Currency,
    string Status,                      // "captured", "pending", "failed"
    string? FailureReason,
    string ReceiptUrl,                  // Link to receipt/proof
    string? ReceiptNumber,              // Merchant receipt number
    DateTime CapturedAt,
    DateTime CreatedAt);

/// <summary>
/// Refund response from payment gateway
/// </summary>
public record PaymentRefund(
    Guid Id,
    string ExternalRefundId,
    Guid ChargeId,
    decimal Amount,
    string Currency,
    string Status,                      // "refunded", "pending", "failed"
    string Reason,                      // "customer_request", "fraud", "duplicate", etc.
    string? FailureReason,
    string? ReceiptUrl,
    DateTime ProcessedAt,
    DateTime CreatedAt);

/// <summary>
/// Void authorization response
/// </summary>
public record PaymentVoid(
    Guid Id,
    string ExternalVoidId,
    Guid AuthorizationId,
    string Reason,
    string Status,                      // "voided", "pending", "failed"
    string? FailureReason,
    DateTime VoidedAt,
    DateTime CreatedAt);

/// <summary>
/// Transaction status from gateway
/// </summary>
public record PaymentStatus(
    string ExternalTransactionId,
    string State,                       // "authorized", "captured", "refunded", "void", "failed"
    decimal Amount,
    string Currency,
    DateTime? CapturedAt,
    DateTime? RefundedAt,
    DateTime? VoidedAt,
    string? LastMessage,
    DateTime RetrievedAt);

/// <summary>
/// Transaction for reconciliation list
/// </summary>
public record PaymentTransaction(
    string ExternalId,
    string Type,                        // "charge", "refund", "adjustment"
    decimal Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    string? CardLast4,
    string? CardBrand,
    DateTime TransactionDate,
    string? Description);

/// <summary>
/// Supported payment method info
/// </summary>
public record PaymentMethodInfo(
    string Method,                      // "card", "apple_pay", "eft", etc.
    string DisplayName,                 // "Credit Card", "Apple Pay", etc.
    decimal FeePercentage,              // 2.9, 1.5, etc.
    decimal MinimumAmount,              // Minimum transaction amount
    decimal MaximumAmount,              // Maximum transaction amount
    List<string> SupportedCurrencies,   // ["ZAR", "USD"]
    bool IsAvailable);
