using FluentAssertions;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Tests.Unit.Domain;

public class FolioTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();
    private static readonly Guid BookingId = Guid.NewGuid();
    private static readonly Guid GuestId = Guid.NewGuid();

    private static Folio CreateFolio() => Folio.Create(PropertyId, BookingId, GuestId, "F-20260101-0001");

    [Fact]
    public void Create_SetsAllProperties()
    {
        var folio = CreateFolio();
        folio.PropertyId.Should().Be(PropertyId);
        folio.BookingId.Should().Be(BookingId);
        folio.GuestId.Should().Be(GuestId);
        folio.FolioNumber.Should().Be("F-20260101-0001");
        folio.Status.Should().Be(FolioStatus.Open);
        folio.TotalCharges.Should().Be(0);
        folio.TotalPayments.Should().Be(0);
        folio.Balance.Should().Be(0);
    }

    [Fact]
    public void Create_RaisesFolioCreatedEvent()
    {
        var folio = CreateFolio();
        folio.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<FolioCreatedEvent>();
    }

    [Fact]
    public void AddCharge_IncreasesTotalCharges()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room Night", 1500, ChargeCategory.RoomCharge);
        folio.TotalCharges.Should().Be(1500);
        folio.Balance.Should().Be(1500);
        folio.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void AddCharge_MultipleCharges_AccumulatesCorrectly()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room Night", 1500, ChargeCategory.RoomCharge);
        folio.AddCharge("Minibar", 250, ChargeCategory.Minibar);
        folio.AddCharge("Breakfast x2", 150, ChargeCategory.Breakfast, 2);
        folio.TotalCharges.Should().Be(1500 + 250 + (150 * 2));
        folio.LineItems.Should().HaveCount(3);
    }

    [Fact]
    public void AddCharge_ClosedFolio_Throws()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room", 1000, ChargeCategory.RoomCharge);
        var payment = Payment.Create(PropertyId, folio.Id, 1000, PaymentMethod.CreditCard);
        folio.RecordPayment(payment);
        folio.Close(Guid.NewGuid());

        var act = () => folio.AddCharge("Late", 100, ChargeCategory.Other);
        act.Should().Throw<InvalidOperationException>().WithMessage("*closed folio*");
    }

    [Fact]
    public void RecordPayment_UpdatesTotalPayments()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room", 2000, ChargeCategory.RoomCharge);
        var payment = Payment.Create(PropertyId, folio.Id, 1000, PaymentMethod.Cash);
        folio.RecordPayment(payment);

        folio.TotalPayments.Should().Be(1000);
        folio.Balance.Should().Be(1000); // 2000 - 1000
    }

    [Fact]
    public void RecordPayment_RaisesEvent()
    {
        var folio = CreateFolio();
        folio.ClearDomainEvents();
        var payment = Payment.Create(PropertyId, folio.Id, 500, PaymentMethod.DebitCard);
        folio.RecordPayment(payment);

        folio.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PaymentRecordedEvent>();
    }

    [Fact]
    public void RecordPayment_ClosedFolio_Throws()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room", 500, ChargeCategory.RoomCharge);
        folio.RecordPayment(Payment.Create(PropertyId, folio.Id, 500, PaymentMethod.Cash));
        folio.Close(Guid.NewGuid());

        var act = () => folio.RecordPayment(Payment.Create(PropertyId, folio.Id, 100, PaymentMethod.Cash));
        act.Should().Throw<InvalidOperationException>().WithMessage("*closed folio*");
    }

    [Fact]
    public void Close_ZeroBalance_Succeeds()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room", 1000, ChargeCategory.RoomCharge);
        folio.RecordPayment(Payment.Create(PropertyId, folio.Id, 1000, PaymentMethod.Cash));
        folio.Close(Guid.NewGuid());

        folio.Status.Should().Be(FolioStatus.Closed);
        folio.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Close_OutstandingBalance_Throws()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room", 1000, ChargeCategory.RoomCharge);
        folio.RecordPayment(Payment.Create(PropertyId, folio.Id, 500, PaymentMethod.Cash));

        var act = () => folio.Close(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*outstanding balance*");
    }

    [Fact]
    public void Close_SmallRoundingDifference_Succeeds()
    {
        var folio = CreateFolio();
        folio.AddCharge("Room", 1000.01m, ChargeCategory.RoomCharge);
        folio.RecordPayment(Payment.Create(PropertyId, folio.Id, 1000, PaymentMethod.Cash));
        // Balance is 0.01 which equals the threshold
        folio.Close(Guid.NewGuid());
        folio.Status.Should().Be(FolioStatus.Closed);
    }

    [Fact]
    public void VoidedLineItems_ExcludedFromTotal()
    {
        var folio = CreateFolio();
        var li = folio.AddCharge("Room", 1000, ChargeCategory.RoomCharge);
        folio.AddCharge("Minibar", 200, ChargeCategory.Minibar);

        li.Void("Wrong charge", Guid.NewGuid());

        // Force recalculation by adding another charge
        folio.AddCharge("Late checkout", 100, ChargeCategory.LateCheckOut);

        // After voiding room (1000) and adding late checkout (100), total should be minibar + late = 300
        folio.TotalCharges.Should().Be(300);
    }
}

public class FolioLineItemTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var li = FolioLineItem.Create(Guid.NewGuid(), "Room Night", 1500, ChargeCategory.RoomCharge, 2);
        li.Description.Should().Be("Room Night");
        li.UnitPrice.Should().Be(1500);
        li.Category.Should().Be(ChargeCategory.RoomCharge);
        li.Quantity.Should().Be(2);
        li.TotalAmount.Should().Be(3000);
        li.VATAmount.Should().Be(450); // 15% of 3000
        li.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void Void_SetsVoidedAndReason()
    {
        var li = FolioLineItem.Create(Guid.NewGuid(), "Test", 100, ChargeCategory.Other);
        li.Void("Mistake", Guid.NewGuid());

        li.IsVoided.Should().BeTrue();
        li.VoidReason.Should().Be("Mistake");
    }
}

public class PaymentTests
{
    [Fact]
    public void Create_ValidInput_SetsAllProperties()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 1500, PaymentMethod.CreditCard, "REF-001");
        payment.Amount.Should().Be(1500);
        payment.Method.Should().Be(PaymentMethod.CreditCard);
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.TransactionReference.Should().Be("REF-001");
        payment.IsRefund.Should().BeFalse();
        payment.Currency.Should().Be("ZAR");
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        var act = () => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 0, PaymentMethod.Cash);
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        var act = () => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), -100, PaymentMethod.Cash);
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void Create_NoReference_GeneratesOne()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 100, PaymentMethod.Cash);
        payment.TransactionReference.Should().StartWith("PAY-");
    }

    [Fact]
    public void CreateRefund_SetsRefundFields()
    {
        var originalId = Guid.NewGuid();
        var refund = Payment.CreateRefund(Guid.NewGuid(), Guid.NewGuid(), 500, PaymentMethod.BankTransfer, originalId, "Guest complaint");

        refund.IsRefund.Should().BeTrue();
        refund.OriginalPaymentId.Should().Be(originalId);
        refund.RefundReason.Should().Be("Guest complaint");
    }

    [Fact]
    public void MarkFailed_UpdatesStatusAndNotes()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 100, PaymentMethod.PayFast);
        payment.MarkFailed("Gateway timeout");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.Notes.Should().Be("Gateway timeout");
    }
}

public class InvoiceTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "INV-2026-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30),
            10000, 1500, 100, "VAT-12345");

        invoice.InvoiceNumber.Should().Be("INV-2026-001");
        invoice.SubtotalAmount.Should().Be(10000);
        invoice.VATAmount.Should().Be(1500);
        invoice.TourismLevyAmount.Should().Be(100);
        invoice.TotalAmount.Should().Be(11600);
        invoice.OutstandingAmount.Should().Be(11600);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.VATNumber.Should().Be("VAT-12345");
    }

    [Fact]
    public void FinalizeInvoice_ChangesStatusToIssued()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000, 150, 10);
        invoice.FinalizeInvoice();
        invoice.Status.Should().Be(InvoiceStatus.Issued);
    }

    [Fact]
    public void MarkPaid_FullAmount_StatusPaid()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000, 150, 10);
        invoice.MarkPaid(1160);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.OutstandingAmount.Should().BeLessThanOrEqualTo(0.01m);
    }

    [Fact]
    public void MarkPaid_PartialAmount_StatusPartiallyPaid()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000, 150, 10);
        invoice.MarkPaid(500);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.OutstandingAmount.Should().Be(660);
    }

    [Fact]
    public void Void_UnpaidInvoice_StatusVoided()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000, 150, 10);
        invoice.Void("Duplicate");
        invoice.Status.Should().Be(InvoiceStatus.Voided);
    }

    [Fact]
    public void Void_PaidInvoice_Throws()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1000, 150, 10);
        invoice.MarkPaid(1160);

        var act = () => invoice.Void("Attempt");
        act.Should().Throw<InvalidOperationException>().WithMessage("*paid invoice*credit note*");
    }
}
