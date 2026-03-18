namespace SAFARIstack.API.Contracts.Payments;

/// <summary>
/// Request to authorize a payment (pre-auth without capture)
/// </summary>
public record AuthorizePaymentRequest(
    decimal Amount,
    string Currency,
    string PaymentMethodId,             // Token from Stripe/Square
    string Description,
    string IdempotencyKey);

/// <summary>
/// Response with authorization details
/// </summary>
public record AuthorizePaymentResponse(
    Guid AuthorizationId,
    string ExternalAuthorizationId,
    decimal Amount,
    string Currency,
    string Status,                      // "authorized", "declined"
    string? FailureReason,
    DateTime ExpiresAt,
    string PaymentMethod,               // "card", "apple_pay"
    string? CardLast4,
    string? CardBrand,
    DateTime CreatedAt);

/// <summary>
/// Request to capture a pre-authorized payment
/// </summary>
public record CapturePaymentRequest(
    Guid AuthorizationId,
    decimal? Amount = null);            // null = capture full authorization

/// <summary>
/// Charge response (authorization captured)
/// </summary>
public record ChargeResponse(
    Guid ChargeId,
    string ExternalChargeId,
    decimal Amount,
    string Currency,
    string Status,                      // "captured", "pending", "failed"
    string? FailureReason,
    string ReceiptUrl,
    string? ReceiptNumber,
    DateTime CapturedAt,
    DateTime CreatedAt);

/// <summary>
/// Request to charge immediately (auth + capture)
/// </summary>
public record ChargePaymentRequest(
    decimal Amount,
    string Currency,
    string PaymentMethodId,             // Token from gateway
    string Description,
    string IdempotencyKey);

/// <summary>
/// Request to refund a charge
/// </summary>
public record RefundPaymentRequest(
    Guid ChargeId,
    decimal? Amount = null,             // null = full refund
    string Reason = "customer_request");

/// <summary>
/// Refund response
/// </summary>
public record RefundResponse(
    Guid RefundId,
    string ExternalRefundId,
    Guid ChargeId,
    decimal Amount,
    string Currency,
    string Status,                      // "refunded", "pending", "failed"
    string Reason,
    string? FailureReason,
    string? ReceiptUrl,
    DateTime ProcessedAt,
    DateTime CreatedAt);

/// <summary>
/// Request to void a pre-authorization
/// </summary>
public record VoidAuthorizationRequest(
    Guid AuthorizationId,
    string Reason = "merchant_cancel");

/// <summary>
/// Void response
/// </summary>
public record VoidResponse(
    Guid VoidId,
    string ExternalVoidId,
    Guid AuthorizationId,
    string Reason,
    string Status,
    DateTime VoidedAt,
    DateTime CreatedAt);

/// <summary>
/// Request to get payment status
/// </summary>
public record PaymentStatusRequest(
    string ExternalTransactionId);

/// <summary>
/// Payment status response
/// </summary>
public record PaymentStatusResponse(
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
/// Request to list transactions for reconciliation
/// </summary>
public record ListPaymentsRequest(
    DateTime FromDate,
    DateTime ToDate,
    List<string>? Statuses = null);

/// <summary>
/// Transaction summary for reconciliation
/// </summary>
public record PaymentTransactionResponse(
    string ExternalId,
    string Type,                        // "charge", "refund"
    decimal Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    string? CardLast4,
    string? CardBrand,
    DateTime TransactionDate,
    string? Description);

/// <summary>
/// Request to apply payment to sale/invoice
/// </summary>
public record ApplyPaymentRequest(
    Guid ChargeId,
    List<ApplyToItem> ApplyToItems,
    string? Notes = null);

/// <summary>
/// Item to apply payment to
/// </summary>
public record ApplyToItem(
    string Type,                        // "casualsale", "invoice"
    Guid Id,
    decimal Amount);

/// <summary>
/// Response showing payment applications
/// </summary>
public record PaymentApplicationResponse(
    Guid ApplicationId,
    Guid ChargeId,
    string AppliedToType,
    Guid AppliedToId,
    decimal AppliedAmount,
    DateTime AppliedAt);

/// <summary>
/// Request to manually reconcile payments
/// </summary>
public record ReconcilePaymentsRequest(
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// Reconciliation report response
/// </summary>
public record ReconciliationReportResponse(
    int TotalCharges,
    decimal TotalChargeAmount,
    int TotalRefunds,
    decimal TotalRefundAmount,
    int TotalApplications,
    decimal TotalApplicationAmount,
    int OutstandingCharges,
    decimal OutstandingAmount,
    int DiscrepanciesFound,
    List<DiscrepancySummary> Discrepancies,
    decimal ReconciliationPercentage,
    DateTime GeneratedAt);

/// <summary>
/// Summary of discrepancy
/// </summary>
public record DiscrepancySummary(
    Guid DiscrepancyId,
    string Type,                        // "missing", "duplicate", "amount_mismatch"
    decimal Amount,
    string Status,
    DateTime ReportedAt);

/// <summary>
/// Request to report payment discrepancy
/// </summary>
public record ReportDiscrepancyRequest(
    string Type,                        // "missing", "duplicate", "amount_mismatch", "timing"
    decimal? Amount,
    string? ChargeId,
    DateTime DiscrepancyDate,
    string Description);

/// <summary>
/// Response confirming discrepancy report
/// </summary>
public record DiscrepancyResponse(
    Guid DiscrepancyId,
    string Type,
    decimal? Amount,
    string? ChargeId,
    DateTime DiscrepancyDate,
    string Description,
    string Status,                      // "open"
    DateTime ReportedAt);

/// <summary>
/// Request to resolve discrepancy
/// </summary>
public record ResolveDiscrepancyRequest(
    string ResolutionType,              // "write_off", "correction", "reversal", "no_action"
    decimal? AdjustmentAmount,
    string Reason);

/// <summary>
/// Response showing resolution
/// </summary>
public record DiscrepancyResolutionResponse(
    Guid DiscrepancyId,
    string Status,                      // "resolved"
    string ResolutionType,
    decimal? AdjustmentAmount,
    DateTime ResolvedAt);

/// <summary>
/// Settlement schedule response
/// </summary>
public record SettlementScheduleResponse(
    DateTime FromDate,
    DateTime ToDate,
    List<SettlementBatchResponse> Batches,
    decimal TotalSettlementAmount,
    decimal TotalFees,
    decimal NetSettled,
    string SettlementCycle,
    DateTime NextSettlementDate,
    DateTime GeneratedAt);

/// <summary>
/// Single settlement batch
/// </summary>
public record SettlementBatchResponse(
    string SettlementId,
    DateTime SettlementDate,
    DateTime DepositDate,
    decimal SettlementAmount,
    decimal Fees,
    int TransactionCount,
    string Status);

/// <summary>
/// Fee calculation response
/// </summary>
public record FeeCalculationResponse(
    Guid ChargeId,
    decimal GrossAmount,
    decimal ProcessingFeePercentage,
    decimal ProcessingFeeAmount,
    decimal? ChargebackReserveAmount,
    decimal NetAmount,
    string GatewayProvider,
    DateTime CalculatedAt);

/// <summary>
/// Outstanding payment balance
/// </summary>
public record OutstandingBalanceResponse(
    decimal TotalOutstanding,
    decimal CurrentMonth,
    decimal PreviousMonth,
    decimal Over30Days,
    decimal Over60Days,
    int UnmatchedTransactionCount,
    DateTime CalculatedAt);

/// <summary>
/// Payment method info
/// </summary>
public record PaymentMethodInfoResponse(
    string Method,                      // "card", "apple_pay", "google_pay", "eft"
    string DisplayName,
    decimal FeePercentage,
    decimal MinimumAmount,
    decimal MaximumAmount,
    List<string> SupportedCurrencies,
    bool IsAvailable);

/// <summary>
/// Generic error response for payment operations
/// </summary>
public record PaymentErrorResponse(
    string Code,                        // "PAYMENT_DECLINED", "GATEWAY_ERROR"
    string Message,
    string? Details = null,
    DateTime Timestamp = default);
