using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Exceptions.Payments;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services.Payments;

/// <summary>
/// Reconciliation service for matching payments against invoices/sales
/// Identifies discrepancies and validates settlement
/// </summary>
public class PaymentReconciliationService : IPaymentReconciliationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentReconciliationService> _logger;

    public PaymentReconciliationService(
        ApplicationDbContext db,
        ILogger<PaymentReconciliationService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reconcile payments for a date range
    /// </summary>
    public async Task<PaymentReconciliationReport> ReconcileAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate)
            throw new ArgumentException("toDate must be >= fromDate");

        _logger.LogInformation(
            "Starting reconciliation for property {PropertyId} from {FromDate} to {ToDate}",
            propertyId, fromDate, toDate);

        // Get all charges in date range
        var charges = await _db.Set<PaymentChargeRecord>()
            .Where(c => c.PropertyId == propertyId &&
                        c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        // Get all refunds in date range
        var refunds = await _db.Set<PaymentRefundRecord>()
            .Where(r => r.PropertyId == propertyId &&
                        r.CreatedAt >= fromDate && r.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        // Get all applications
        var applications = await _db.Set<PaymentApplicationRecord>()
            .Where(a => a.PropertyId == propertyId &&
                        a.AppliedAt >= fromDate && a.AppliedAt <= toDate)
            .ToListAsync(cancellationToken);

        // Get existing discrepancies
        var discrepancies = await _db.Set<PaymentDiscrepancyRecord>()
            .Where(d => d.PropertyId == propertyId &&
                        d.ReportedAt >= fromDate && d.ReportedAt <= toDate)
            .ToListAsync(cancellationToken);

        decimal totalChargeAmount = charges.Sum(c => c.Amount);
        decimal totalRefundAmount = refunds.Sum(r => r.Amount);
        decimal totalApplicationAmount = applications.Sum(a => a.AppliedAmount);

        // Calculate reconciliation percentage
        decimal appliedAmount = applications
            .GroupBy(a => a.ChargeId)
            .Sum(g => g.Sum(x => x.AppliedAmount));

        decimal outstandingAmount = totalChargeAmount - totalRefundAmount - appliedAmount;
        int outstandingCharges = charges.Count(c => !applications.Any(a => a.ChargeId == c.Id));

        decimal reconciliationPercentage = totalChargeAmount > 0
            ? (appliedAmount / totalChargeAmount) * 100
            : 0;

        var report = new PaymentReconciliationReport(
            PropertyId: propertyId,
            FromDate: fromDate,
            ToDate: toDate,
            TotalCharges: charges.Count,
            TotalChargeAmount: totalChargeAmount,
            TotalRefunds: refunds.Count,
            TotalRefundAmount: totalRefundAmount,
            TotalApplications: applications.Count,
            TotalApplicationAmount: totalApplicationAmount,
            OutstandingCharges: outstandingCharges,
            OutstandingAmount: outstandingAmount,
            DiscrepanciesFound: discrepancies.Count,
            Discrepancies: discrepancies.Select(d => new PaymentDiscrepancySummary(
                d.Id,
                d.Type,
                d.Amount ?? 0,
                d.Status,
                d.ReportedAt
            )).ToList(),
            ReconciliationPercentage: reconciliationPercentage,
            GeneratedAt: DateTime.UtcNow);

        _logger.LogInformation(
            "Reconciliation complete: {Total} charges, {Applied}% matched, {Outstanding} outstanding",
            charges.Count, reconciliationPercentage.ToString("F1"), outstandingCharges);

        return report;
    }

    /// <summary>
    /// Get payment application history
    /// </summary>
    public async Task<List<PaymentApplication>> GetPaymentApplicationHistoryAsync(
        Guid chargeId,
        CancellationToken cancellationToken = default)
    {
        var applications = await _db.Set<PaymentApplicationRecord>()
            .Where(a => a.ChargeId == chargeId)
            .OrderBy(a => a.Sequence)
            .ToListAsync(cancellationToken);

        return applications.Select(a => new PaymentApplication(
            a.Id,
            a.ChargeId,
            a.AppliedToType,
            a.AppliedToId,
            a.AppliedAmount,
            a.Sequence,
            a.AppliedAt,
            a.AppliedByStaffId
        )).ToList();
    }

    /// <summary>
    /// Apply payment to invoices/sales
    /// </summary>
    public async Task<List<PaymentApplication>> ApplyPaymentAsync(
        Guid chargeId,
        List<ApplyPaymentRequest> applications,
        Guid appliedByStaffId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var charge = await _db.Set<PaymentChargeRecord>()
            .FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken)
            ?? throw new ArgumentException("Charge not found", nameof(chargeId));

        decimal remainingAmount = charge.Amount;
        var appliedRecords = new List<PaymentApplicationRecord>();
        int sequence = 0;

        foreach (var app in applications)
        {
            if (remainingAmount <= 0)
                break;

            decimal applyAmount = Math.Min(app.Amount, remainingAmount);

            var appRecord = new PaymentApplicationRecord
            {
                Id = Guid.NewGuid(),
                PropertyId = charge.PropertyId,
                ChargeId = chargeId,
                AppliedToType = app.Type,
                AppliedToId = app.Id,
                AppliedAmount = applyAmount,
                Sequence = ++sequence,
                AppliedAt = DateTime.UtcNow,
                AppliedByStaffId = appliedByStaffId,
                Notes = notes
            };

            appliedRecords.Add(appRecord);
            remainingAmount -= applyAmount;
        }

        _db.Set<PaymentApplicationRecord>().AddRange(appliedRecords);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {ChargeId} applied to {Count} items, remaining: {Remaining}",
            chargeId, appliedRecords.Count, remainingAmount);

        return appliedRecords.Select(a => new PaymentApplication(
            a.Id, a.ChargeId, a.AppliedToType, a.AppliedToId,
            a.AppliedAmount, a.Sequence, a.AppliedAt, a.AppliedByStaffId
        )).ToList();
    }

    /// <summary>
    /// Calculate payment processing fees
    /// </summary>
    public async Task<PaymentFeeCalculation> CalculateFeesAsync(
        Guid chargeId,
        CancellationToken cancellationToken = default)
    {
        var charge = await _db.Set<PaymentChargeRecord>()
            .FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken)
            ?? throw new ArgumentException("Charge not found", nameof(chargeId));

        // Fee percentages by gateway (configurable in future)
        decimal processingFeePercentage = 2.9m;  // 2.9% + $0.30 typical Stripe
        decimal processingFeeAmount = (charge.Amount * processingFeePercentage / 100m);
        decimal chargebackReserveAmount = charge.Amount * 0.01m; // 1% reserve

        decimal netAmount = charge.Amount - processingFeeAmount - chargebackReserveAmount;

        var calculation = new PaymentFeeCalculation(
            ChargeId: chargeId,
            GrossAmount: charge.Amount,
            ProcessingFeePercentage: processingFeePercentage,
            ProcessingFeeAmount: Math.Round(processingFeeAmount, 2),
            ChargebackReserveAmount: Math.Round(chargebackReserveAmount, 2),
            NetAmount: Math.Round(netAmount, 2),
            GatewayProvider: "stripe",
            CalculatedAt: DateTime.UtcNow);

        _logger.LogInformation(
            "Fee calculation for charge {ChargeId}: Gross {Gross}, Fees {Fees}, Net {Net}",
            chargeId, charge.Amount, processingFeeAmount, netAmount);

        return calculation;
    }

    /// <summary>
    /// Get settlement schedule
    /// </summary>
    public async Task<PaymentSettlementReport> GetSettlementScheduleAsync(
        Guid propertyId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var charges = await _db.Set<PaymentChargeRecord>()
            .Where(c => c.PropertyId == propertyId &&
                        c.CreatedAt >= fromDate && c.CreatedAt <= toDate &&
                        c.Status == "captured")
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        // Group by settlement batches (typically daily)
        var batches = new List<SettlementBatch>();
        foreach (var chargesOnDate in charges.GroupBy(c => c.CreatedAt.Date))
        {
            decimal batchAmount = chargesOnDate.Sum(c => c.Amount);
            decimal batchFees = batchAmount * 0.029m; // 2.9%

            batches.Add(new SettlementBatch(
                SettlementId: $"settlement_{chargesOnDate.Key:yyyy_MM_dd}",
                SettlementDate: chargesOnDate.Key,
                DepositDate: chargesOnDate.Key.AddDays(1), // T+1
                SettlementAmount: Math.Round(batchAmount, 2),
                Fees: Math.Round(batchFees, 2),
                TransactionCount: chargesOnDate.Count(),
                Status: "completed"
            ));
        }

        decimal totalAmount = charges.Sum(c => c.Amount);
        decimal totalFees = totalAmount * 0.029m;

        var report = new PaymentSettlementReport(
            PropertyId: propertyId,
            FromDate: fromDate,
            ToDate: toDate,
            Batches: batches,
            TotalSettlementAmount: Math.Round(totalAmount, 2),
            TotalFees: Math.Round(totalFees, 2),
            NetSettled: Math.Round(totalAmount - totalFees, 2),
            SettlementCycle: "daily",
            NextSettlementDate: DateTime.UtcNow.AddDays(1),
            GeneratedAt: DateTime.UtcNow);

        _logger.LogInformation(
            "Settlement schedule for {PropertyId}: {Batches} batches, total {Total}",
            propertyId, batches.Count, totalAmount);

        return report;
    }

    /// <summary>
    /// Report a payment discrepancy
    /// </summary>
    public async Task<PaymentDiscrepancy> ReportDiscrepancyAsync(
        ReportDiscrepancyRequest discrepancy,
        Guid reportedByStaffId,
        CancellationToken cancellationToken = default)
    {
        var record = new PaymentDiscrepancyRecord
        {
            Id = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(), // Would be passed in from controller
            Type = discrepancy.Type,
            Amount = discrepancy.Amount,
            ChargeId = discrepancy.ChargeId,
            DiscrepancyDate = discrepancy.DiscrepancyDate,
            Description = discrepancy.Description,
            Status = "open",
            ReportedAt = DateTime.UtcNow,
            ReportedByStaffId = reportedByStaffId
        };

        _db.Set<PaymentDiscrepancyRecord>().Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Discrepancy reported: {Type} Amount:{Amount} by staff {StaffId}",
            discrepancy.Type, discrepancy.Amount, reportedByStaffId);

        return new PaymentDiscrepancy(
            record.Id,
            record.PropertyId,
            record.Type,
            record.Amount,
            record.ChargeId,
            record.DiscrepancyDate,
            record.Description,
            record.Status,
            record.ReportedAt,
            record.ReportedByStaffId,
            null, null, null, null, null);
    }

    /// <summary>
    /// Resolve a discrepancy
    /// </summary>
    public async Task<PaymentDiscrepancy> ResolveDiscrepancyAsync(
        Guid discrepancyId,
        ResolveDiscrepancyRequest resolution,
        Guid resolvedByStaffId,
        CancellationToken cancellationToken = default)
    {
        var discrepancy = await _db.Set<PaymentDiscrepancyRecord>()
            .FirstOrDefaultAsync(d => d.Id == discrepancyId, cancellationToken)
            ?? throw new ArgumentException("Discrepancy not found", nameof(discrepancyId));

        discrepancy.Status = "resolved";
        discrepancy.ResolutionType = resolution.ResolutionType;
        discrepancy.ResolutionAmount = resolution.AdjustmentAmount;
        discrepancy.ResolvedAt = DateTime.UtcNow;
        discrepancy.ResolvedByStaffId = resolvedByStaffId;
        discrepancy.ResolutionNotes = resolution.Reason;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Discrepancy {DiscrepancyId} resolved as {ResolutionType} by staff {StaffId}",
            discrepancyId, resolution.ResolutionType, resolvedByStaffId);

        return new PaymentDiscrepancy(
            discrepancy.Id,
            discrepancy.PropertyId,
            discrepancy.Type,
            discrepancy.Amount,
            discrepancy.ChargeId,
            discrepancy.DiscrepancyDate,
            discrepancy.Description,
            discrepancy.Status,
            discrepancy.ReportedAt,
            discrepancy.ReportedByStaffId,
            discrepancy.ResolutionType,
            discrepancy.ResolutionAmount,
            discrepancy.ResolvedAt,
            discrepancy.ResolvedByStaffId,
            discrepancy.ResolutionNotes);
    }

    /// <summary>
    /// Get outstanding payment balance
    /// </summary>
    public async Task<OutstandingPaymentBalance> GetOutstandingBalanceAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        // Get unapplied charges
        var chargesNotApplied = await _db.Set<PaymentChargeRecord>()
            .Where(c => c.PropertyId == propertyId && c.Status == "captured")
            .ToListAsync(cancellationToken);

        var appliedCharges = await _db.Set<PaymentApplicationRecord>()
            .Where(a => a.PropertyId == propertyId)
            .GroupBy(a => a.ChargeId)
            .Select(g => new { ChargeId = g.Key, TotalApplied = g.Sum(a => a.AppliedAmount) })
            .ToListAsync(cancellationToken);

        decimal totalOutstanding = 0;
        decimal currentMonth = 0;
        decimal previousMonth = 0;
        decimal over30Days = 0;
        decimal over60Days = 0;
        int unmatchedCount = 0;

        var today = DateTime.UtcNow;

        foreach (var charge in chargesNotApplied)
        {
            var applied = appliedCharges
                .FirstOrDefault(a => a.ChargeId == charge.Id)?.TotalApplied ?? 0;

            decimal outstanding = charge.Amount - applied;
            if (outstanding <= 0) continue;

            totalOutstanding += outstanding;
            unmatchedCount++;

            int daysOld = (today - charge.CreatedAt).Days;
            if (daysOld <= 30)
                currentMonth += outstanding;
            else if (daysOld <= 60)
                previousMonth += outstanding;
            else
                over60Days += outstanding;
        }

        over30Days = previousMonth + over60Days;

        var balance = new OutstandingPaymentBalance(
            PropertyId: propertyId,
            TotalOutstanding: Math.Round(totalOutstanding, 2),
            CurrentMonth: Math.Round(currentMonth, 2),
            PreviousMonth: Math.Round(previousMonth, 2),
            Over30Days: Math.Round(over30Days, 2),
            Over60Days: Math.Round(over60Days, 2),
            UnmatchedTransactionCount: unmatchedCount,
            CalculatedAt: today);

        return balance;
    }
}

/// <summary>
/// Database records for reconciliation
/// </summary>
public class PaymentApplicationRecord
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ChargeId { get; set; }
    public string AppliedToType { get; set; } = ""; // casualsale, invoice
    public Guid AppliedToId { get; set; }
    public decimal AppliedAmount { get; set; }
    public int Sequence { get; set; }
    public DateTime AppliedAt { get; set; }
    public Guid? AppliedByStaffId { get; set; }
    public string? Notes { get; set; }
}

public class PaymentDiscrepancyRecord
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Type { get; set; } = ""; // missing, duplicate, amount_mismatch
    public decimal? Amount { get; set; }
    public string? ChargeId { get; set; }
    public DateTime DiscrepancyDate { get; set; }
    public string Description { get; set; } = "";
    public string Status { get; set; } = "open"; // open, investigating, resolved
    public DateTime ReportedAt { get; set; }
    public Guid ReportedByStaffId { get; set; }
    public string? ResolutionType { get; set; } // write_off, correction, reversal
    public decimal? ResolutionAmount { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByStaffId { get; set; }
    public string? ResolutionNotes { get; set; }
}
