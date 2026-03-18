namespace SAFARIstack.Shared.ValueObjects;

/// <summary>
/// Strong-typed Money value object with SA financial compliance (ZAR, VAT, Tourism Levy)
/// </summary>
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    // SA-specific constants
    public const decimal SA_VAT_RATE = 0.15m;
    public const decimal SA_TOURISM_LEVY_RATE = 0.01m;
    public const string DEFAULT_CURRENCY = "ZAR";

    private Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money FromZAR(decimal amount) => new(amount, DEFAULT_CURRENCY);
    public static Money FromAmount(decimal amount, string currency = DEFAULT_CURRENCY) => new(amount, currency);
    public static Money Zero(string currency = DEFAULT_CURRENCY) => new(0, currency);

    /// <summary>
    /// Calculate VAT amount (15% for South Africa)
    /// </summary>
    public Money CalculateVAT() => new(Amount * SA_VAT_RATE, Currency);

    /// <summary>
    /// Calculate Tourism Levy (1% for South Africa)
    /// </summary>
    public Money CalculateTourismLevy() => new(Amount * SA_TOURISM_LEVY_RATE, Currency);

    /// <summary>
    /// Calculate total including VAT
    /// </summary>
    public Money WithVAT() => new(Amount * (1 + SA_VAT_RATE), Currency);

    /// <summary>
    /// Calculate total including both VAT and Tourism Levy
    /// </summary>
    public Money WithVATAndTourismLevy() => new(Amount * (1 + SA_VAT_RATE + SA_TOURISM_LEVY_RATE), Currency);

    /// <summary>
    /// Extract VAT from total (VAT-inclusive price)
    /// </summary>
    public Money ExtractVAT() => new(Amount - (Amount / (1 + SA_VAT_RATE)), Currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);
    public static Money operator *(decimal factor, Money money) => money.Multiply(factor);

    public override string ToString() => $"{Currency} {Amount:N2}";
}
