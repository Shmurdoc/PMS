using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Exceptions.Payments;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services.Payments;

/// <summary>
/// Pluggable payment gateway service supporting multiple providers (Stripe, Square, PayFast)
/// Provides unified interface for authorization, capture, refunds
/// </summary>
public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentGatewayService> _logger;
    private readonly IPaymentGatewayProvider _provider;

    public PaymentGatewayService(
        ApplicationDbContext db,
        ILogger<PaymentGatewayService> logger,
        IPaymentGatewayProvider provider)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Authorize a payment without capturing (pre-authorization)
    /// </summary>
    public async Task<PaymentAuthorization> AuthorizeAsync(
        Guid propertyId,
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (amount <= 0)
            throw new InvalidTransactionAmountException(amount);

        if (string.IsNullOrWhiteSpace(currency))
            throw new UnsupportedCurrencyException(currency ?? "");

        if (string.IsNullOrWhiteSpace(paymentMethodId))
            throw new ArgumentException("Payment method ID required", nameof(paymentMethodId));

        // Check for duplicate idempotency key
        var existingAuth = await _db.Set<PaymentAuthorizationRecord>()
            .FirstOrDefaultAsync(
                a => a.IdempotencyKey == idempotencyKey && a.PropertyId == propertyId,
                cancellationToken);

        if (existingAuth != null)
        {
            _logger.LogWarning("Duplicate authorization attempt: {IdempotencyKey} for property {PropertyId}",
                idempotencyKey, propertyId);
            throw new DuplicatePaymentException(idempotencyKey, existingAuth.ExternalAuthorizationId);
        }

        try
        {
            // Call payment provider
            var providerResult = await _provider.AuthorizeAsync(
                amount, currency, paymentMethodId, description, idempotencyKey, cancellationToken);

            // Store in database
            var authRecord = new PaymentAuthorizationRecord
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                ExternalAuthorizationId = providerResult.ExternalAuthorizationId,
                Amount = amount,
                Currency = currency,
                Status = providerResult.Status,
                FailureReason = providerResult.FailureReason,
                ExpiresAt = providerResult.ExpiresAt,
                CardLast4 = providerResult.CardLast4,
                CardBrand = providerResult.CardBrand,
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<PaymentAuthorizationRecord>().Add(authRecord);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Authorization created: {AuthId} Property:{PropertyId} Amount:{Amount} {Currency} Status:{Status}",
                authRecord.Id, propertyId, amount, currency, providerResult.Status);

            return new PaymentAuthorization(
                authRecord.Id,
                providerResult.ExternalAuthorizationId,
                propertyId,
                amount,
                currency,
                providerResult.Status,
                providerResult.FailureReason,
                providerResult.ExpiresAt,
                "card",
                providerResult.CardLast4,
                providerResult.CardBrand,
                authRecord.CreatedAt);
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error during authorization for property {PropertyId}", propertyId);
            throw;
        }
        catch (PaymentDeclinedException ex)
        {
            _logger.LogWarning("Payment declined: {Reason}", ex.DeclineReason);
            throw;
        }
    }

    /// <summary>
    /// Capture a previously authorized payment
    /// </summary>
    public async Task<PaymentCapture> CaptureAsync(
        Guid authorizationId,
        decimal? amount = null,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _db.Set<PaymentAuthorizationRecord>()
            .FirstOrDefaultAsync(a => a.Id == authorizationId, cancellationToken)
            ?? throw new InvalidAuthorizationException(authorizationId, "not found");

        if (authorization.Status != "authorized")
            throw new InvalidAuthorizationException(authorizationId, $"status is {authorization.Status}");

        if (authorization.ExpiresAt < DateTime.UtcNow)
            throw new InvalidAuthorizationException(authorizationId, "expired");

        decimal captureAmount = amount ?? authorization.Amount;
        if (captureAmount > authorization.Amount)
            throw new InvalidTransactionAmountException(captureAmount, null, authorization.Amount);

        try
        {
            // Call provider
            var providerResult = await _provider.CaptureAsync(
                authorization.ExternalAuthorizationId, captureAmount, cancellationToken);

            // Store charge
            var chargeRecord = new PaymentChargeRecord
            {
                Id = Guid.NewGuid(),
                PropertyId = authorization.PropertyId,
                AuthorizationId = authorizationId,
                ExternalChargeId = providerResult.ExternalChargeId,
                Amount = captureAmount,
                Currency = authorization.Currency,
                Status = providerResult.Status,
                ReceiptUrl = providerResult.ReceiptUrl,
                ReceiptNumber = providerResult.ReceiptNumber,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<PaymentChargeRecord>().Add(chargeRecord);

            // Mark authorization as captured
            authorization.Status = "captured";
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Capture completed: {ChargeId} Amount:{Amount} Status:{Status}",
                chargeRecord.Id, captureAmount, providerResult.Status);

            return new PaymentCapture(
                chargeRecord.Id,
                providerResult.ExternalChargeId,
                authorization.PropertyId,
                authorizationId,
                captureAmount,
                authorization.Currency,
                providerResult.Status,
                providerResult.FailureReason,
                providerResult.ReceiptUrl,
                providerResult.ReceiptNumber,
                DateTime.UtcNow,
                chargeRecord.CreatedAt);
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error during capture of authorization {AuthId}", authorizationId);
            throw;
        }
    }

    /// <summary>
    /// Charge payment immediately (authorize + capture in one operation)
    /// </summary>
    public async Task<PaymentCapture> ChargeAsync(
        Guid propertyId,
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // Validate
        if (amount <= 0)
            throw new InvalidTransactionAmountException(amount);

        // Check duplicate
        var existingCharge = await _db.Set<PaymentChargeRecord>()
            .FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey && c.PropertyId == propertyId,
                cancellationToken);

        if (existingCharge != null)
            throw new DuplicatePaymentException(idempotencyKey, existingCharge.ExternalChargeId);

        try
        {
            // Call provider
            var providerResult = await _provider.ChargeAsync(
                amount, currency, paymentMethodId, description, idempotencyKey, cancellationToken);

            // Store charge
            var chargeRecord = new PaymentChargeRecord
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                ExternalChargeId = providerResult.ExternalChargeId,
                Amount = amount,
                Currency = currency,
                Status = providerResult.Status,
                ReceiptUrl = providerResult.ReceiptUrl,
                ReceiptNumber = providerResult.ReceiptNumber,
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<PaymentChargeRecord>().Add(chargeRecord);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Charge completed: {ChargeId} Property:{PropertyId} Amount:{Amount} Status:{Status}",
                chargeRecord.Id, propertyId, amount, providerResult.Status);

            return new PaymentCapture(
                chargeRecord.Id,
                providerResult.ExternalChargeId,
                propertyId,
                null,
                amount,
                currency,
                providerResult.Status,
                providerResult.FailureReason,
                providerResult.ReceiptUrl,
                providerResult.ReceiptNumber,
                DateTime.UtcNow,
                chargeRecord.CreatedAt);
        }
        catch (PaymentDeclinedException ex)
        {
            _logger.LogWarning("Charge declined: {Reason}", ex.DeclineReason);
            throw;
        }
    }

    /// <summary>
    /// Refund a captured payment
    /// </summary>
    public async Task<PaymentRefund> RefundAsync(
        Guid chargeId,
        decimal? amount = null,
        string reason = "customer_request",
        CancellationToken cancellationToken = default)
    {
        var charge = await _db.Set<PaymentChargeRecord>()
            .FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken)
            ?? throw new ArgumentException("Charge not found", nameof(chargeId));

        if (charge.Status != "captured")
            throw new PaymentProcessingException("Charge not captured", "CHARGE_NOT_CAPTURED");

        decimal refundAmount = amount ?? charge.Amount;
        if (refundAmount > charge.Amount)
            throw new InvalidRefundAmountException(chargeId, charge.Amount, refundAmount);

        try
        {
            var providerResult = await _provider.RefundAsync(
                charge.ExternalChargeId, refundAmount, reason, cancellationToken);

            var refundRecord = new PaymentRefundRecord
            {
                Id = Guid.NewGuid(),
                ChargeId = chargeId,
                PropertyId = charge.PropertyId,
                ExternalRefundId = providerResult.ExternalRefundId,
                Amount = refundAmount,
                Currency = charge.Currency,
                Status = providerResult.Status,
                Reason = reason,
                ReceiptUrl = providerResult.ReceiptUrl,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<PaymentRefundRecord>().Add(refundRecord);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refund processed: {RefundId} Charge:{ChargeId} Amount:{Amount} Status:{Status}",
                refundRecord.Id, chargeId, refundAmount, providerResult.Status);

            return new PaymentRefund(
                refundRecord.Id,
                providerResult.ExternalRefundId,
                chargeId,
                refundAmount,
                charge.Currency,
                providerResult.Status,
                reason,
                providerResult.FailureReason,
                providerResult.ReceiptUrl,
                DateTime.UtcNow,
                refundRecord.CreatedAt);
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error during refund of charge {ChargeId}", chargeId);
            throw;
        }
    }

    /// <summary>
    /// Void a pre-authorization
    /// </summary>
    public async Task<PaymentVoid> VoidAsync(
        Guid authorizationId,
        string reason = "merchant_cancel",
        CancellationToken cancellationToken = default)
    {
        var auth = await _db.Set<PaymentAuthorizationRecord>()
            .FirstOrDefaultAsync(a => a.Id == authorizationId, cancellationToken)
            ?? throw new InvalidAuthorizationException(authorizationId);

        if (auth.Status != "authorized")
            throw new InvalidAuthorizationException(authorizationId, $"cannot void {auth.Status}");

        try
        {
            var providerResult = await _provider.VoidAsync(auth.ExternalAuthorizationId, reason, cancellationToken);

            var voidRecord = new PaymentVoidRecord
            {
                Id = Guid.NewGuid(),
                AuthorizationId = authorizationId,
                PropertyId = auth.PropertyId,
                ExternalVoidId = providerResult.ExternalVoidId,
                Reason = reason,
                Status = providerResult.Status,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<PaymentVoidRecord>().Add(voidRecord);
            auth.Status = "voided";
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Authorization voided: {VoidId} Auth:{AuthId}", voidRecord.Id, authorizationId);

            return new PaymentVoid(
                voidRecord.Id,
                providerResult.ExternalVoidId,
                authorizationId,
                reason,
                providerResult.Status,
                providerResult.FailureReason,
                DateTime.UtcNow,
                voidRecord.CreatedAt);
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error during void of authorization {AuthId}", authorizationId);
            throw;
        }
    }

    /// <summary>
    /// Get transaction status from gateway
    /// </summary>
    public async Task<PaymentStatus> GetTransactionStatusAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalTransactionId))
            throw new ArgumentException("Transaction ID required", nameof(externalTransactionId));

        try
        {
            var status = await _provider.GetTransactionStatusAsync(externalTransactionId, cancellationToken);
            _logger.LogInformation("Retrieved status for transaction {TransactionId}: {Status}",
                externalTransactionId, status.State);
            return status;
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error retrieving status for {TransactionId}", externalTransactionId);
            throw;
        }
    }

    /// <summary>
    /// List transactions for reconciliation
    /// </summary>
    public async Task<List<PaymentTransaction>> ListTransactionsAsync(
        Guid? propertyId,
        DateTime fromDate,
        DateTime toDate,
        List<string>? statuses = null,
        CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate)
            throw new ArgumentException("toDate must be >= fromDate");

        try
        {
            var transactions = await _provider.ListTransactionsAsync(
                propertyId, fromDate, toDate, statuses, cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} transactions for property {PropertyId} from {FromDate} to {ToDate}",
                transactions.Count, propertyId, fromDate, toDate);

            return transactions;
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error listing transactions");
            throw;
        }
    }

    /// <summary>
    /// Verify webhook signature
    /// </summary>
    public async Task<bool> VerifyWebhookSignatureAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
            return false;

        try
        {
            var isValid = await _provider.VerifyWebhookSignatureAsync(payload, signature, cancellationToken);
            if (!isValid)
            {
                _logger.LogWarning("Webhook signature verification failed - potential spoofing attempt");
                throw new InvalidWebhookSignatureException();
            }
            return isValid;
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error verifying webhook signature");
            throw;
        }
    }

    /// <summary>
    /// Get supported payment methods
    /// </summary>
    public async Task<List<PaymentMethodInfo>> GetSupportedMethodsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var methods = await _provider.GetSupportedMethodsAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} supported payment methods", methods.Count);
            return methods;
        }
        catch (PaymentGatewayException)
        {
            _logger.LogError("Gateway error retrieving supported methods");
            throw;
        }
    }
}

/// <summary>
/// Abstraction for specific payment gateway providers (Stripe, Square, PayFast)
/// </summary>
public interface IPaymentGatewayProvider
{
    Task<PaymentAuthorizationResult> AuthorizeAsync(
        decimal amount, string currency, string paymentMethodId, string description,
        string idempotencyKey, CancellationToken cancellationToken);

    Task<PaymentCaptureResult> CaptureAsync(
        string externalAuthorizationId, decimal amount, CancellationToken cancellationToken);

    Task<PaymentCaptureResult> ChargeAsync(
        decimal amount, string currency, string paymentMethodId, string description,
        string idempotencyKey, CancellationToken cancellationToken);

    Task<PaymentRefundResult> RefundAsync(
        string externalChargeId, decimal amount, string reason, CancellationToken cancellationToken);

    Task<PaymentVoidResult> VoidAsync(
        string externalAuthorizationId, string reason, CancellationToken cancellationToken);

    Task<PaymentStatus> GetTransactionStatusAsync(
        string externalTransactionId, CancellationToken cancellationToken);

    Task<List<PaymentTransaction>> ListTransactionsAsync(
        Guid? propertyId, DateTime fromDate, DateTime toDate, List<string>? statuses,
        CancellationToken cancellationToken);

    Task<bool> VerifyWebhookSignatureAsync(
        string payload, string signature, CancellationToken cancellationToken);

    Task<List<PaymentMethodInfo>> GetSupportedMethodsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Provider-specific result types
/// </summary>
public record PaymentAuthorizationResult(
    string ExternalAuthorizationId,
    string Status,
    string? FailureReason,
    DateTime ExpiresAt,
    string? CardLast4,
    string? CardBrand);

public record PaymentCaptureResult(
    string ExternalChargeId,
    string Status,
    string? FailureReason,
    string ReceiptUrl,
    string? ReceiptNumber);

public record PaymentRefundResult(
    string ExternalRefundId,
    string Status,
    string? FailureReason,
    string? ReceiptUrl);

public record PaymentVoidResult(
    string ExternalVoidId,
    string Status,
    string? FailureReason);

/// <summary>
/// Database records for payment operations (for Phase 3 integration)
/// These will be mapped to entities in a future migration
/// </summary>
public class PaymentAuthorizationRecord
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string ExternalAuthorizationId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "authorized"; // authorized, captured, voided, expired
    public string? FailureReason { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class PaymentChargeRecord
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? AuthorizationId { get; set; }  // If from pre-auth
    public string ExternalChargeId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "captured"; // captured, failed
    public string? FailureReason { get; set; }
    public string ReceiptUrl { get; set; } = "";
    public string? ReceiptNumber { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentRefundRecord
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ChargeId { get; set; }
    public string ExternalRefundId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "refunded"; // refunded, failed
    public string? FailureReason { get; set; }
    public string Reason { get; set; } = "customer_request";
    public string? ReceiptUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentVoidRecord
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid AuthorizationId { get; set; }
    public string ExternalVoidId { get; set; } = "";
    public string Reason { get; set; } = "merchant_cancel";
    public string Status { get; set; } = "voided";
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
