namespace SAFARIstack.Core.Domain.Services;

/// <summary>
/// Reconciliation service for matching payments against invoices/sales
/// Identifies discrepancies and settlement dates
/// </summary>
public interface IPaymentReconciliationService
{
    /// <summary>
    /// Reconcile payments for a date range
    /// Matches receipts with actual bank deposits and identifies discrepancies
    /// </summary>
    /// <param name="propertyId">Property to reconcile</param>
    /// <param name="fromDate">Start date (UTC)</param>
    /// <param name="toDate">End date (UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconciliation report with discrepancies</returns>
    Task<PaymentReconciliationReport> ReconcileAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment application history (which payment applied to which invoice)
    /// </summary>
    /// <param name="chargeId">Charge ID to trace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applications (invoices, sales, adjustments)</returns>
    Task<List<PaymentApplication>> GetPaymentApplicationHistoryAsync(
        Guid chargeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply payment to invoices/sales (manual matching for discrepancies)
    /// </summary>
    /// <param name="chargeId">Charge to apply</param>
    /// <param name="applications">List of items to apply payment to</param>
    /// <param name="appliedByStaffId">Who/what system applied payment</param>
    /// <param name="notes">Notes on manual application</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated applications</returns>
    Task<List<PaymentApplication>> ApplyPaymentAsync(
        Guid chargeId,
        List<ApplyPaymentRequest> applications,
        Guid appliedByStaffId,
        string? notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate payment processing fees
    /// </summary>
    /// <param name="chargeId">Charge to calculate fees for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fee breakdown</returns>
    Task<PaymentFeeCalculation> CalculateFeesAsync(
        Guid chargeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get settlement information (when money hits bank account)
    /// </summary>
    /// <param name="propertyId">Property for settlement</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settlement schedule</returns>
    Task<PaymentSettlementReport> GetSettlementScheduleAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Report discrepancy (payment missing, amount mismatch)
    /// </summary>
    /// <param name="discrepancy">Request describing discrepancy</param>
    /// <param name="reportedByStaffId">Who reported issue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discrepancy record for investigation</returns>
    Task<PaymentDiscrepancy> ReportDiscrepancyAsync(
        ReportDiscrepancyRequest discrepancy,
        Guid reportedByStaffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a discrepancy (write-off, correction, adjustment)
    /// </summary>
    /// <param name="discrepancyId">Discrepancy to resolve</param>
    /// <param name="resolution">Resolution details (write-off, correction, etc.)</param>
    /// <param name="resolvedByStaffId">Who resolved it</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved discrepancy record</returns>
    Task<PaymentDiscrepancy> ResolveDiscrepancyAsync(
        Guid discrepancyId,
        ResolveDiscrepancyRequest resolution,
        Guid resolvedByStaffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get outstanding (unapplied) payment balance
    /// </summary>
    /// <param name="propertyId">Property</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total unapplied amount and aging</returns>
    Task<OutstandingPaymentBalance> GetOutstandingBalanceAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reconciliation report showing matches and discrepancies
/// </summary>
public record PaymentReconciliationReport(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    int TotalCharges,
    decimal TotalChargeAmount,
    int TotalRefunds,
    decimal TotalRefundAmount,
    int TotalApplications,
    decimal TotalApplicationAmount,
    int OutstandingCharges,
    decimal OutstandingAmount,
    int DiscrepanciesFound,
    List<PaymentDiscrepancySummary> Discrepancies,
    decimal ReconciliationPercentage,   // % of charges matched
    DateTime GeneratedAt);

/// <summary>
/// Summary of a discrepancy
/// </summary>
public record PaymentDiscrepancySummary(
    Guid DiscrepancyId,
    string Type,                        // "missing", "duplicate", "amount_mismatch", "timing"
    decimal Amount,
    string Status,                      // "open", "investigating", "resolved"
    DateTime ReportedAt);

/// <summary>
/// Payment application (which payment applied to which doc)
/// </summary>
public record PaymentApplication(
    Guid Id,
    Guid ChargeId,
    string AppliedToType,               // "casualsale", "invoice", "deposit", "adjustment"
    Guid AppliedToId,
    decimal AppliedAmount,
    int Sequence,                       // Order of application if partial
    DateTime AppliedAt,
    Guid? AppliedByStaffId);

/// <summary>
/// Request to apply payment to documents
/// </summary>
public record ApplyPaymentRequest(
    string Type,                        // "casualsale", "invoice"
    Guid Id,
    decimal Amount);

/// <summary>
/// Fee breakdown for payment
/// </summary>
public record PaymentFeeCalculation(
    Guid ChargeId,
    decimal GrossAmount,
    decimal ProcessingFeePercentage,    // 2.9%, 1.5%, etc.
    decimal ProcessingFeeAmount,
    decimal? ChargebackReserveAmount,    // Stripe/Square reserve
    decimal NetAmount,                  // Amount that hits account
    string GatewayProvider,             // "stripe", "square", "payfirst"
    DateTime CalculatedAt);

/// <summary>
/// Settlement schedule (when money arrives in bank)
/// </summary>
public record PaymentSettlementReport(
    Guid PropertyId,
    DateTime FromDate,
    DateTime ToDate,
    List<SettlementBatch> Batches,
    decimal TotalSettlementAmount,
    decimal TotalFees,
    decimal NetSettled,
    string SettlementCycle,             // "daily", "weekly", "monthly"
    DateTime NextSettlementDate,
    DateTime GeneratedAt);

/// <summary>
/// Single settlement batch (typically 1 per business day)
/// </summary>
public record SettlementBatch(
    string SettlementId,
    DateTime SettlementDate,
    DateTime DepositDate,
    decimal SettlementAmount,
    decimal Fees,
    int TransactionCount,
    string Status);                     // "pending", "completed", "failed"

/// <summary>
/// Discrepancy record (investigation required)
/// </summary>
public record PaymentDiscrepancy(
    Guid Id,
    Guid PropertyId,
    string Type,                        // "missing", "duplicate", "amount_mismatch", "timing", "other"
    decimal? Amount,
    string? ChargeId,
    DateTime DiscrepancyDate,
    string Description,
    string Status,                      // "open", "investigating", "resolved"
    DateTime ReportedAt,
    Guid ReportedByStaffId,
    string? ResolutionType,             // "write_off", "correction", "reversal"
    decimal? ResolutionAmount,
    DateTime? ResolvedAt,
    Guid? ResolvedByStaffId,
    string? ResolutionNotes);

/// <summary>
/// Request to report a payment discrepancy
/// </summary>
public record ReportDiscrepancyRequest(
    string Type,                        // "missing", "duplicate", "amount_mismatch", "timing"
    decimal? Amount,
    string? ChargeId,
    DateTime DiscrepancyDate,
    string Description);

/// <summary>
/// Request to resolve a discrepancy
/// </summary>
public record ResolveDiscrepancyRequest(
    string ResolutionType,              // "write_off", "correction", "reversal", "no_action"
    decimal? AdjustmentAmount,
    string Reason);

/// <summary>
/// Outstanding payment balance
/// </summary>
public record OutstandingPaymentBalance(
    Guid PropertyId,
    decimal TotalOutstanding,
    decimal CurrentMonth,               // Not yet matched
    decimal PreviousMonth,              // Older outstanding
    decimal Over30Days,
    decimal Over60Days,
    int UnmatchedTransactionCount,
    DateTime CalculatedAt);
