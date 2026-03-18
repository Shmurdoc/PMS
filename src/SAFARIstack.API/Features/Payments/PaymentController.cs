using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAFARIstack.API.Contracts.Payments;
using SAFARIstack.Core.Domain.Exceptions.Payments;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Services.Payments;

namespace SAFARIstack.API.Features.Payments;

/// <summary>
/// Payment Processing and Reconciliation API
/// Handles payment authorization, capture, refunds, and reconciliation
/// All endpoints require [Authorize] - sensitive ops require Manager role
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IPaymentReconciliationService _reconciliation;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentGatewayService paymentGateway,
        IPaymentReconciliationService reconciliation,
        ILogger<PaymentController> logger)
    {
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _reconciliation = reconciliation ?? throw new ArgumentNullException(nameof(reconciliation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authorize a payment (pre-auth without capture)
    /// Authorization is held for 7 days, then expires
    /// </summary>
    /// <param name="propertyId">Property initiating payment</param>
    /// <param name="request">Authorization request with payment method token</param>
    /// <returns>Authorization with expiration date</returns>
    [HttpPost("authorize")]
    public async Task<ActionResult<AuthorizePaymentResponse>> AuthorizePayment(
        [FromQuery] Guid propertyId,
        [FromBody] AuthorizePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auth = await _paymentGateway.AuthorizeAsync(
                propertyId, request.Amount, request.Currency, request.PaymentMethodId,
                request.Description, request.IdempotencyKey, cancellationToken);

            return Ok(new AuthorizePaymentResponse(
                AuthorizationId: auth.Id,
                ExternalAuthorizationId: auth.ExternalAuthorizationId,
                Amount: auth.Amount,
                Currency: auth.Currency,
                Status: auth.Status,
                FailureReason: auth.FailureReason,
                ExpiresAt: auth.ExpiresAt,
                PaymentMethod: auth.PaymentMethod,
                CardLast4: auth.CardLast4,
                CardBrand: auth.CardBrand,
                CreatedAt: auth.CreatedAt));
        }
        catch (PaymentDeclinedException ex)
        {
            _logger.LogWarning("Payment declined: {Reason}", ex.DeclineReason);
            return StatusCode(402, new PaymentErrorResponse("PAYMENT_DECLINED", ex.Message));
        }
        catch (DuplicatePaymentException ex)
        {
            _logger.LogWarning("Duplicate payment: {IdempotencyKey}", ex.IdempotencyKey);
            return Conflict(new PaymentErrorResponse("DUPLICATE_PAYMENT", ex.Message));
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Payment gateway error");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message, ex.RequestId));
        }
    }

    /// <summary>
    /// Capture a previously authorized payment
    /// Move funds from authorization hold to actual charge
    /// </summary>
    /// <param name="request">Capture request with authorization ID</param>
    /// <returns>Charge response with receipt details</returns>
    [HttpPost("capture")]
    public async Task<ActionResult<ChargeResponse>> CapturePayment(
        [FromBody] CapturePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var charge = await _paymentGateway.CaptureAsync(
                request.AuthorizationId, request.Amount, cancellationToken);

            return Ok(new ChargeResponse(
                ChargeId: charge.Id,
                ExternalChargeId: charge.ExternalChargeId,
                Amount: charge.Amount,
                Currency: charge.Currency,
                Status: charge.Status,
                FailureReason: charge.FailureReason,
                ReceiptUrl: charge.ReceiptUrl,
                ReceiptNumber: charge.ReceiptNumber,
                CapturedAt: charge.CapturedAt,
                CreatedAt: charge.CreatedAt));
        }
        catch (InvalidAuthorizationException ex)
        {
            _logger.LogWarning("Invalid authorization: {AuthId}", ex.AuthorizationId);
            return Conflict(new PaymentErrorResponse("INVALID_AUTHORIZATION", ex.Message));
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Gateway error capturing payment");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Charge payment immediately (authorize + capture in one call)
    /// </summary>
    /// <param name="propertyId">Property initiating payment</param>
    /// <param name="request">Charge request with payment method token</param>
    /// <returns>Charge response with receipt link</returns>
    [HttpPost("charge")]
    public async Task<ActionResult<ChargeResponse>> ChargePayment(
        [FromQuery] Guid propertyId,
        [FromBody] ChargePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var charge = await _paymentGateway.ChargeAsync(
                propertyId, request.Amount, request.Currency, request.PaymentMethodId,
                request.Description, request.IdempotencyKey, cancellationToken);

            return CreatedAtAction(nameof(GetPaymentStatus), new { transactionId = charge.ExternalChargeId },
                new ChargeResponse(
                    ChargeId: charge.Id,
                    ExternalChargeId: charge.ExternalChargeId,
                    Amount: charge.Amount,
                    Currency: charge.Currency,
                    Status: charge.Status,
                    FailureReason: charge.FailureReason,
                    ReceiptUrl: charge.ReceiptUrl,
                    ReceiptNumber: charge.ReceiptNumber,
                    CapturedAt: charge.CapturedAt,
                    CreatedAt: charge.CreatedAt));
        }
        catch (PaymentDeclinedException ex)
        {
            return StatusCode(402, new PaymentErrorResponse("PAYMENT_DECLINED", ex.Message));
        }
        catch (DuplicatePaymentException ex)
        {
            return Conflict(new PaymentErrorResponse("DUPLICATE_PAYMENT", ex.Message));
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Gateway error during charge");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Refund a captured charge (full or partial)
    /// Requires Manager role
    /// </summary>
    /// <param name="request">Refund request with charge ID and amount</param>
    /// <returns>Refund response with status</returns>
    [HttpPost("refund")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<RefundResponse>> RefundPayment(
        [FromBody] RefundPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _paymentGateway.RefundAsync(
                request.ChargeId, request.Amount, request.Reason, cancellationToken);

            return Ok(new RefundResponse(
                RefundId: refund.Id,
                ExternalRefundId: refund.ExternalRefundId,
                ChargeId: refund.ChargeId,
                Amount: refund.Amount,
                Currency: refund.Currency,
                Status: refund.Status,
                Reason: refund.Reason,
                FailureReason: refund.FailureReason,
                ReceiptUrl: refund.ReceiptUrl,
                ProcessedAt: refund.ProcessedAt,
                CreatedAt: refund.CreatedAt));
        }
        catch (InvalidRefundAmountException ex)
        {
            _logger.LogWarning("Invalid refund: {Amount} exceeds charge {ChargeAmount}",
                ex.RefundAmount, ex.ChargeAmount);
            return Conflict(new PaymentErrorResponse("INVALID_REFUND_AMOUNT", ex.Message));
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Gateway error refunding payment");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Void a pre-authorization (release hold on funds)
    /// Requires Manager role
    /// </summary>
    /// <param name="authorizationId">Authorization ID to void</param>
    /// <returns>Void confirmation</returns>
    [HttpPost("void/{authorizationId}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<VoidResponse>> VoidAuthorization(
        Guid authorizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var voidResult = await _paymentGateway.VoidAsync(
                authorizationId, "merchant_cancel", cancellationToken);

            return Ok(new VoidResponse(
                VoidId: voidResult.Id,
                ExternalVoidId: voidResult.ExternalVoidId,
                AuthorizationId: voidResult.AuthorizationId,
                Reason: voidResult.Reason,
                Status: voidResult.Status,
                VoidedAt: voidResult.VoidedAt,
                CreatedAt: voidResult.CreatedAt));
        }
        catch (InvalidAuthorizationException ex)
        {
            return Conflict(new PaymentErrorResponse("INVALID_AUTHORIZATION", ex.Message));
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Gateway error voiding authorization");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Get payment transaction status
    /// Query external gateway for current state
    /// </summary>
    /// <param name="transactionId">External transaction ID from gateway</param>
    /// <returns>Current payment status</returns>
    [HttpGet("status/{transactionId}")]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatus(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _paymentGateway.GetTransactionStatusAsync(transactionId, cancellationToken);

            return Ok(new PaymentStatusResponse(
                ExternalTransactionId: status.ExternalTransactionId,
                State: status.State,
                Amount: status.Amount,
                Currency: status.Currency,
                CapturedAt: status.CapturedAt,
                RefundedAt: status.RefundedAt,
                VoidedAt: status.VoidedAt,
                LastMessage: status.LastMessage,
                RetrievedAt: status.RetrievedAt));
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Gateway error retrieving status");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// List transactions for reconciliation
    /// Filters by date range and optional status
    /// Requires Manager role
    /// </summary>
    /// <param name="propertyId">Property to list transactions for</param>
    /// <param name="request">Date range and filters</param>
    /// <returns>List of transactions</returns>
    [HttpPost("list")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<List<PaymentTransactionResponse>>> ListPayments(
        [FromQuery] Guid propertyId,
        [FromBody] ListPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transactions = await _paymentGateway.ListTransactionsAsync(
                propertyId, request.FromDate, request.ToDate, request.Statuses, cancellationToken);

            return Ok(transactions.Select(t => new PaymentTransactionResponse(
                ExternalId: t.ExternalId,
                Type: t.Type,
                Amount: t.Amount,
                Currency: t.Currency,
                Status: t.Status,
                PaymentMethod: t.PaymentMethod,
                CardLast4: t.CardLast4,
                CardBrand: t.CardBrand,
                TransactionDate: t.TransactionDate,
                Description: t.Description
            )).ToList());
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Gateway error listing transactions");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Reconcile payments for a date range
    /// Matches charges against applications and identifies discrepancies
    /// Requires Manager role
    /// </summary>
    /// <param name="propertyId">Property to reconcile</param>
    /// <param name="request">Date range</param>
    /// <returns>Reconciliation report</returns>
    [HttpPost("reconcile")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ReconciliationReportResponse>> ReconcilePayments(
        [FromQuery] Guid propertyId,
        [FromBody] ReconcilePaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _reconciliation.ReconcileAsync(
                propertyId, request.FromDate, request.ToDate, cancellationToken);

            return Ok(new ReconciliationReportResponse(
                TotalCharges: report.TotalCharges,
                TotalChargeAmount: report.TotalChargeAmount,
                TotalRefunds: report.TotalRefunds,
                TotalRefundAmount: report.TotalRefundAmount,
                TotalApplications: report.TotalApplications,
                TotalApplicationAmount: report.TotalApplicationAmount,
                OutstandingCharges: report.OutstandingCharges,
                OutstandingAmount: report.OutstandingAmount,
                DiscrepanciesFound: report.DiscrepanciesFound,
                Discrepancies: report.Discrepancies.Select(d => new DiscrepancySummary(
                    d.DiscrepancyId, d.Type, d.Amount, d.Status, d.ReportedAt
                )).ToList(),
                ReconciliationPercentage: report.ReconciliationPercentage,
                GeneratedAt: report.GeneratedAt));
        }
        catch (ReconciliationMismatchException ex)
        {
            _logger.LogWarning("Reconciliation mismatch: {Expected} vs {Actual}",
                ex.ExpectedAmount, ex.ActualAmount);
            return Conflict(new PaymentErrorResponse("RECONCILIATION_MISMATCH", ex.Message));
        }
    }

    /// <summary>
    /// Apply payment to sales/invoices (manual matching)
    /// Used to resolve discrepancies during reconciliation
    /// Requires Manager role
    /// </summary>
    /// <param name="propertyId">Property for payment</param>
    /// <param name="request">Payment and items to apply to</param>
    /// <returns>List of applications created</returns>
    [HttpPost("apply")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<List<PaymentApplicationResponse>>> ApplyPayment(
        [FromQuery] Guid propertyId,
        [FromBody] SAFARIstack.API.Contracts.Payments.ApplyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: staffId would come from claims in production
            var staffId = Guid.NewGuid();

            // Map ApplyToItem DTO to service request format
            var serviceApplyRequests = request.ApplyToItems.Select(item =>
                new SAFARIstack.Core.Domain.Services.ApplyPaymentRequest(
                    Type: item.Type,
                    Id: item.Id,
                    Amount: item.Amount
                )
            ).ToList();

            var applications = await _reconciliation.ApplyPaymentAsync(
                request.ChargeId,
                serviceApplyRequests,
                staffId,
                request.Notes,
                cancellationToken);

            return Ok(applications.Select(a => new PaymentApplicationResponse(
                ApplicationId: a.Id,
                ChargeId: a.ChargeId,
                AppliedToType: a.AppliedToType,
                AppliedToId: a.AppliedToId,
                AppliedAmount: a.AppliedAmount,
                AppliedAt: a.AppliedAt
            )).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying payment");
            return BadRequest(new PaymentErrorResponse("APPLICATION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Report payment discrepancy
    /// Documents missing, duplicate, or amount mismatch payments
    /// Requires Manager role
    /// </summary>
    /// <param name="propertyId">Property with discrepancy</param>
    /// <param name="request">Discrepancy details</param>
    /// <returns>Discrepancy record created</returns>
    [HttpPost("discrepancy")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<DiscrepancyResponse>> ReportDiscrepancy(
        [FromQuery] Guid propertyId,
        [FromBody] SAFARIstack.API.Contracts.Payments.ReportDiscrepancyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var staffId = Guid.NewGuid();

            var serviceRequest = new SAFARIstack.Core.Domain.Services.ReportDiscrepancyRequest(
                Type: request.Type,
                Amount: request.Amount,
                ChargeId: request.ChargeId,
                DiscrepancyDate: request.DiscrepancyDate,
                Description: request.Description
            );

            var discrepancy = await _reconciliation.ReportDiscrepancyAsync(
                serviceRequest, staffId, cancellationToken);

            return CreatedAtAction(nameof(GetPaymentStatus), new { transactionId = discrepancy.ChargeId },
                new DiscrepancyResponse(
                    DiscrepancyId: discrepancy.Id,
                    Type: discrepancy.Type,
                    Amount: discrepancy.Amount,
                    ChargeId: discrepancy.ChargeId,
                    DiscrepancyDate: discrepancy.DiscrepancyDate,
                    Description: discrepancy.Description,
                    Status: discrepancy.Status,
                    ReportedAt: discrepancy.ReportedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting discrepancy");
            return BadRequest(new PaymentErrorResponse("REPORT_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Get settlement schedule
    /// Shows when payments will arrive in bank account
    /// Requires Manager role
    /// </summary>
    /// <param name="propertyId">Property for settlement</param>
    /// <param name="request">Date range</param>
    /// <returns>Settlement schedule with batches</returns>
    [HttpPost("settlement")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<SettlementScheduleResponse>> GetSettlementSchedule(
        [FromQuery] Guid propertyId,
        [FromBody] ReconcilePaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var schedule = await _reconciliation.GetSettlementScheduleAsync(
                propertyId, request.FromDate, request.ToDate, cancellationToken);

            return Ok(new SettlementScheduleResponse(
                FromDate: schedule.FromDate,
                ToDate: schedule.ToDate,
                Batches: schedule.Batches.Select(b => new SettlementBatchResponse(
                    SettlementId: b.SettlementId,
                    SettlementDate: b.SettlementDate,
                    DepositDate: b.DepositDate,
                    SettlementAmount: b.SettlementAmount,
                    Fees: b.Fees,
                    TransactionCount: b.TransactionCount,
                    Status: b.Status
                )).ToList(),
                TotalSettlementAmount: schedule.TotalSettlementAmount,
                TotalFees: schedule.TotalFees,
                NetSettled: schedule.NetSettled,
                SettlementCycle: schedule.SettlementCycle,
                NextSettlementDate: schedule.NextSettlementDate,
                GeneratedAt: schedule.GeneratedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving settlement schedule");
            return BadRequest(new PaymentErrorResponse("SETTLEMENT_ERROR", ex.Message));
        }
    }

    /// <summary>
    /// Get supported payment methods with fees
    /// Shows what payment options are available
    /// </summary>
    /// <returns>List of supported payment methods</returns>
    [HttpGet("methods")]
    public async Task<ActionResult<List<PaymentMethodInfoResponse>>> GetSupportedMethods(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var methods = await _paymentGateway.GetSupportedMethodsAsync(cancellationToken);

            return Ok(methods.Select(m => new PaymentMethodInfoResponse(
                Method: m.Method,
                DisplayName: m.DisplayName,
                FeePercentage: m.FeePercentage,
                MinimumAmount: m.MinimumAmount,
                MaximumAmount: m.MaximumAmount,
                SupportedCurrencies: m.SupportedCurrencies,
                IsAvailable: m.IsAvailable
            )).ToList());
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Gateway error retrieving supported methods");
            return StatusCode(503, new PaymentErrorResponse("GATEWAY_ERROR", ex.Message));
        }
    }
}
