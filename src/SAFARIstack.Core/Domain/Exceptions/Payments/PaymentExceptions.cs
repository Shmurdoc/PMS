using SAFARIstack.Shared.Exceptions;

namespace SAFARIstack.Core.Domain.Exceptions.Payments;

/// <summary>
/// Payment processing failed with specific reason
/// </summary>
public class PaymentProcessingException : DomainException
{
    private readonly string _errorCode;

    public PaymentProcessingException(string message, string errorCode = "PAYMENT_FAILED")
        : base(message)
    {
        _errorCode = errorCode;
    }

    public override string ErrorCode => _errorCode;
    public override int StatusCode => 402; // Payment Required
}

/// <summary>
/// Payment was declined by the payment gateway
/// </summary>
public class PaymentDeclinedException : DomainException
{
    public string? CardBrand { get; set; }              // "visa", "mastercard"
    public string? CardLast4 { get; set; }
    public string? DeclineReason { get; set; }         // "insufficient_funds", "lost_card", etc.

    public PaymentDeclinedException(string message, string? reason = null)
        : base(message)
    {
        DeclineReason = reason;
    }

    public override string ErrorCode => "PAYMENT_DECLINED";
    public override int StatusCode => 402; // Payment Required
}

/// <summary>
/// Authorization expired or invalid
/// </summary>
public class InvalidAuthorizationException : DomainException
{
    public Guid AuthorizationId { get; set; }
    public string? Reason { get; set; }

    public InvalidAuthorizationException(Guid authId, string? reason = null)
        : base($"Authorization {authId} is invalid or expired")
    {
        AuthorizationId = authId;
        Reason = reason;
    }

    public override string ErrorCode => "INVALID_AUTHORIZATION";
    public override int StatusCode => 409; // Conflict
}

/// <summary>
/// Refund amount exceeds original charge amount
/// </summary>
public class InvalidRefundAmountException : DomainException
{
    public Guid ChargeId { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal RefundAmount { get; set; }

    public InvalidRefundAmountException(Guid chargeId, decimal chargeAmount, decimal refundAmount)
        : base($"Refund amount ({refundAmount}) exceeds charge amount ({chargeAmount})")
    {
        ChargeId = chargeId;
        ChargeAmount = chargeAmount;
        RefundAmount = refundAmount;
    }

    public override string ErrorCode => "INVALID_REFUND_AMOUNT";
    public override int StatusCode => 409; // Conflict
}

/// <summary>
/// Duplicate idempotency key detected (prevents duplicate charges)
/// </summary>
public class DuplicatePaymentException : DomainException
{
    public string IdempotencyKey { get; set; }
    public string? ExistingChargeId { get; set; }

    public DuplicatePaymentException(string idempotencyKey, string? existingChargeId = null)
        : base($"Payment with idempotency key '{idempotencyKey}' already processed")
    {
        IdempotencyKey = idempotencyKey;
        ExistingChargeId = existingChargeId;
    }

    public override string ErrorCode => "DUPLICATE_PAYMENT";
    public override int StatusCode => 409; // Conflict
}

/// <summary>
/// Payment gateway API error (timeout, unreachable, etc.)
/// </summary>
public class PaymentGatewayException : DomainException
{
    public string? Gateway { get; set; }                // "stripe", "square", "payfast"
    public string? GatewayErrorCode { get; set; }
    public string? RequestId { get; set; }             // For support tracking

    public PaymentGatewayException(string message, string? gateway = null, 
        string? errorCode = null, string? requestId = null)
        : base(message)
    {
        Gateway = gateway;
        GatewayErrorCode = errorCode;
        RequestId = requestId;
    }

    public override string ErrorCode => "GATEWAY_ERROR";
    public override int StatusCode => 503; // Service Unavailable
}

/// <summary>
/// Payment method (card, account) not supported
/// </summary>
public class UnsupportedPaymentMethodException : DomainException
{
    public string PaymentMethod { get; set; }
    public string? Reason { get; set; }

    public UnsupportedPaymentMethodException(string method, string? reason = null)
        : base($"Payment method '{method}' is not supported")
    {
        PaymentMethod = method;
        Reason = reason;
    }

    public override string ErrorCode => "UNSUPPORTED_METHOD";
    public override int StatusCode => 400; // Bad Request
}

/// <summary>
/// Transaction amount outside acceptable range
/// </summary>
public class InvalidTransactionAmountException : DomainException
{
    public decimal Amount { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaximumAmount { get; set; }

    public InvalidTransactionAmountException(decimal amount, decimal? min = null, decimal? max = null)
        : base(BuildMessage(amount, min, max))
    {
        Amount = amount;
        MinimumAmount = min;
        MaximumAmount = max;
    }

    private static string BuildMessage(decimal amount, decimal? min, decimal? max)
    {
        if (min.HasValue && amount < min.Value)
            return $"Amount {amount} is below minimum {min.Value}";
        else if (max.HasValue && amount > max.Value)
            return $"Amount {amount} exceeds maximum {max.Value}";
        else
            return $"Amount {amount} is invalid";
    }

    public override string ErrorCode => "INVALID_AMOUNT";
    public override int StatusCode => 400; // Bad Request
}

/// <summary>
/// Webhook signature verification failed (prevent spoofing)
/// </summary>
public class InvalidWebhookSignatureException : DomainException
{
    public string? Gateway { get; set; }
    public string? SignatureHeader { get; set; }

    public InvalidWebhookSignatureException(string? gateway = null)
        : base("Webhook signature verification failed - request may be spoofed")
    {
        Gateway = gateway;
    }

    public override string ErrorCode => "INVALID_SIGNATURE";
    public override int StatusCode => 401; // Unauthorized
}

/// <summary>
/// Reconciliation mismatch detected
/// </summary>
public class ReconciliationMismatchException : DomainException
{
    public Guid PropertyId { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public string? Description { get; set; }

    public ReconciliationMismatchException(Guid propertyId, decimal expected, decimal actual, 
        string? description = null)
        : base($"Reconciliation mismatch: expected {expected} but got {actual}")
    {
        PropertyId = propertyId;
        ExpectedAmount = expected;
        ActualAmount = actual;
        Description = description;
    }

    public override string ErrorCode => "RECONCILIATION_MISMATCH";
    public override int StatusCode => 409; // Conflict
}

/// <summary>
/// Currency not supported by payment gateway
/// </summary>
public class UnsupportedCurrencyException : DomainException
{
    public string Currency { get; set; }
    public List<string> SupportedCurrencies { get; set; } = new();

    public UnsupportedCurrencyException(string currency, List<string>? supported = null)
        : base($"Currency '{currency}' is not supported")
    {
        Currency = currency;
        SupportedCurrencies = supported ?? new();
    }

    public override string ErrorCode => "UNSUPPORTED_CURRENCY";
    public override int StatusCode => 400; // Bad Request
}
