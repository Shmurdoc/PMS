namespace SAFARIstack.Shared.ValueObjects;

/// <summary>
/// SA-specific financial calculation with VAT and Tourism Levy breakdown
/// </summary>
public sealed record FinancialBreakdown
{
    public Money Subtotal { get; init; }
    public Money VATAmount { get; init; }
    public Money TourismLevy { get; init; }
    public Money AdditionalCharges { get; init; }
    public Money DiscountAmount { get; init; }
    public Money Total { get; init; }

    private FinancialBreakdown(
        Money subtotal,
        Money vatAmount,
        Money tourismLevy,
        Money additionalCharges,
        Money discountAmount,
        Money total)
    {
        Subtotal = subtotal;
        VATAmount = vatAmount;
        TourismLevy = tourismLevy;
        AdditionalCharges = additionalCharges;
        DiscountAmount = discountAmount;
        Total = total;
    }

    public static FinancialBreakdown Calculate(
        Money subtotal,
        Money? additionalCharges = null,
        Money? discountAmount = null)
    {
        var additional = additionalCharges ?? Money.Zero();
        var discount = discountAmount ?? Money.Zero();

        var adjustedSubtotal = subtotal + additional - discount;
        var vatAmount = adjustedSubtotal.CalculateVAT();
        var tourismLevy = adjustedSubtotal.CalculateTourismLevy();
        var total = adjustedSubtotal + vatAmount + tourismLevy;

        return new FinancialBreakdown(
            adjustedSubtotal,
            vatAmount,
            tourismLevy,
            additional,
            discount,
            total);
    }
}
