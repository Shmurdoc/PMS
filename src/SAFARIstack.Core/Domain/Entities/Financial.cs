using SAFARIstack.Shared.Domain;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
//  FOLIO — Running account for a stay (room charges, extras, payments)
// ═══════════════════════════════════════════════════════════════════════
public class Folio : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid GuestId { get; private set; }
    public string FolioNumber { get; private set; } = string.Empty;     // "F-20260210-0001"
    public FolioStatus Status { get; private set; } = FolioStatus.Open;
    public decimal TotalCharges { get; private set; }
    public decimal TotalPayments { get; private set; }
    public decimal TotalRefunds { get; private set; }
    public decimal Balance => TotalCharges - TotalPayments + TotalRefunds;
    public DateTime? ClosedAt { get; private set; }

    // Navigation
    public Booking Booking { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;
    private readonly List<FolioLineItem> _lineItems = new();
    public IReadOnlyCollection<FolioLineItem> LineItems => _lineItems.AsReadOnly();
    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Folio() { }

    public static Folio Create(Guid propertyId, Guid bookingId, Guid guestId, string folioNumber)
    {
        var folio = new Folio
        {
            PropertyId = propertyId,
            BookingId = bookingId,
            GuestId = guestId,
            FolioNumber = folioNumber
        };
        folio.AddDomainEvent(new FolioCreatedEvent(folio.Id, folioNumber, bookingId));
        return folio;
    }

    public FolioLineItem AddCharge(string description, decimal amount, ChargeCategory category,
        int quantity = 1, Guid? serviceItemId = null)
    {
        if (Status != FolioStatus.Open)
            throw new InvalidOperationException("Cannot add charges to a closed folio.");

        var lineItem = FolioLineItem.Create(Id, description, amount, category, quantity, serviceItemId);
        _lineItems.Add(lineItem);
        RecalculateTotals();
        return lineItem;
    }

    public void RecordPayment(Payment payment)
    {
        if (Status != FolioStatus.Open)
            throw new InvalidOperationException("Cannot record payments on a closed folio.");

        _payments.Add(payment);
        TotalPayments += payment.Amount;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentRecordedEvent(Id, payment.Id, payment.Amount, payment.Method));
    }

    public void Close(Guid? closedByUserId)
    {
        if (Balance > 0.01m)
            throw new InvalidOperationException($"Cannot close folio with outstanding balance of {Balance:C}");

        Status = FolioStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        SetModifiedBy(closedByUserId ?? Guid.Empty);
        AddDomainEvent(new FolioClosedEvent(Id, FolioNumber));
    }

    private void RecalculateTotals()
    {
        TotalCharges = _lineItems.Where(li => !li.IsVoided).Sum(li => li.TotalAmount);
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum FolioStatus { Open, Closed, Disputed, WriteOff }

// ═══════════════════════════════════════════════════════════════════════
//  FOLIO LINE ITEM — Individual charge on a folio
// ═══════════════════════════════════════════════════════════════════════
public class FolioLineItem : AuditableEntity
{
    public Guid FolioId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ChargeCategory Category { get; private set; }
    public int Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal TotalAmount => Quantity * UnitPrice;
    public decimal VATAmount => decimal.Round(TotalAmount * Money.SA_VAT_RATE, 2);
    public Guid? ServiceItemId { get; private set; }                // Link to minibar, laundry, etc.
    public DateTime ChargeDate { get; private set; } = DateTime.UtcNow;
    public bool IsVoided { get; private set; }
    public string? VoidReason { get; private set; }

    // Navigation
    public Folio Folio { get; private set; } = null!;

    private FolioLineItem() { }

    public static FolioLineItem Create(
        Guid folioId, string description, decimal unitPrice,
        ChargeCategory category, int quantity = 1, Guid? serviceItemId = null)
    {
        return new FolioLineItem
        {
            FolioId = folioId,
            Description = description,
            UnitPrice = unitPrice,
            Category = category,
            Quantity = quantity,
            ServiceItemId = serviceItemId
        };
    }

    public void Void(string reason, Guid userId)
    {
        IsVoided = true;
        VoidReason = reason;
        SetModifiedBy(userId);
    }
}

public enum ChargeCategory
{
    RoomCharge,
    ExtraPerson,
    Breakfast,
    Meal,
    Beverage,
    Minibar,
    Laundry,
    Telephone,
    Internet,
    Spa,
    Activity,          // Safari drive, boat cruise, etc.
    Parking,
    RoomService,
    DamageDeposit,
    CancellationFee,
    EarlyCheckIn,
    LateCheckOut,
    Transfer,          // Airport/shuttle
    Other
}

// ═══════════════════════════════════════════════════════════════════════
//  PAYMENT — Financial transaction against a folio
// ═══════════════════════════════════════════════════════════════════════
public class Payment : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid FolioId { get; private set; }
    public Guid? BookingId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = Money.DEFAULT_CURRENCY;
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Completed;
    public string? TransactionReference { get; private set; }       // Gateway ref
    public string? GatewayReference { get; private set; }           // PayFast/Ozow/Yoco ref
    public string? Notes { get; private set; }
    public DateTime PaymentDate { get; private set; } = DateTime.UtcNow;

    // Refund tracking
    public bool IsRefund { get; private set; }
    public Guid? OriginalPaymentId { get; private set; }
    public string? RefundReason { get; private set; }

    // Navigation
    public Folio Folio { get; private set; } = null!;

    private Payment() { }

    public static Payment Create(
        Guid propertyId, Guid folioId, decimal amount,
        PaymentMethod method, string? transactionReference = null,
        Guid? bookingId = null)
    {
        if (amount <= 0) throw new ArgumentException("Payment amount must be positive.");

        return new Payment
        {
            PropertyId = propertyId,
            FolioId = folioId,
            BookingId = bookingId,
            Amount = amount,
            Method = method,
            TransactionReference = transactionReference ?? GenerateReference(),
            PaymentDate = DateTime.UtcNow
        };
    }

    public static Payment CreateRefund(
        Guid propertyId, Guid folioId, decimal amount,
        PaymentMethod method, Guid originalPaymentId, string reason)
    {
        return new Payment
        {
            PropertyId = propertyId,
            FolioId = folioId,
            Amount = amount,
            Method = method,
            IsRefund = true,
            OriginalPaymentId = originalPaymentId,
            RefundReason = reason,
            TransactionReference = GenerateReference(),
            PaymentDate = DateTime.UtcNow
        };
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateReference()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

public enum PaymentMethod
{
    Cash,
    CreditCard,
    DebitCard,
    BankTransfer,       // EFT
    PayFast,
    Ozow,
    Yoco,
    SnapScan,
    Zapper,
    CityLedger,         // Company account
    Voucher,
    Complimentary
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    PartialRefund,
    Disputed,
    Cancelled
}

// ═══════════════════════════════════════════════════════════════════════
//  INVOICE — Tax-compliant document generated from folio
// ═══════════════════════════════════════════════════════════════════════
public class Invoice : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid FolioId { get; private set; }
    public Guid GuestId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;   // "INV-2026-000123"
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public DateTime InvoiceDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal VATAmount { get; private set; }
    public decimal TourismLevyAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount => TotalAmount - PaidAmount;

    // SA Tax compliance
    public string? VATNumber { get; private set; }                      // Property VAT registration
    public string? CompanyName { get; private set; }                    // For corporate invoices
    public string? CompanyVATNumber { get; private set; }               // Guest company VAT

    // Navigation
    public Folio Folio { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;

    private Invoice() { }

    public static Invoice Create(
        Guid propertyId, Guid folioId, Guid guestId,
        string invoiceNumber, DateTime invoiceDate, DateTime dueDate,
        decimal subtotal, decimal vatAmount, decimal tourismLevy,
        string? vatNumber = null)
    {
        var invoice = new Invoice
        {
            PropertyId = propertyId,
            FolioId = folioId,
            GuestId = guestId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            SubtotalAmount = subtotal,
            VATAmount = vatAmount,
            TourismLevyAmount = tourismLevy,
            TotalAmount = subtotal + vatAmount + tourismLevy,
            VATNumber = vatNumber
        };
        invoice.AddDomainEvent(new InvoiceCreatedEvent(invoice.Id, invoiceNumber, invoice.TotalAmount));
        return invoice;
    }

    public void FinalizeInvoice()
    {
        Status = InvoiceStatus.Issued;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new InvoiceFinalizedEvent(Id, InvoiceNumber));
    }

    public void MarkPaid(decimal amount)
    {
        PaidAmount += amount;
        if (OutstandingAmount <= 0.01m)
            Status = InvoiceStatus.Paid;
        else
            Status = InvoiceStatus.PartiallyPaid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Void(string reason)
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot void a paid invoice — issue a credit note instead.");
        Status = InvoiceStatus.Voided;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum InvoiceStatus { Draft, Issued, Paid, PartiallyPaid, Overdue, Voided, CreditNote }

// ─── Domain Events ───────────────────────────────────────────────────
public record FolioCreatedEvent(Guid FolioId, string FolioNumber, Guid BookingId) : DomainEvent;
public record FolioClosedEvent(Guid FolioId, string FolioNumber) : DomainEvent;
public record PaymentRecordedEvent(Guid FolioId, Guid PaymentId, decimal Amount, PaymentMethod Method) : DomainEvent;
public record InvoiceCreatedEvent(Guid InvoiceId, string InvoiceNumber, decimal TotalAmount) : DomainEvent;
public record InvoiceFinalizedEvent(Guid InvoiceId, string InvoiceNumber) : DomainEvent;
