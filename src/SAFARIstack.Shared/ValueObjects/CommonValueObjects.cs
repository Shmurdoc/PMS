namespace SAFARIstack.Shared.ValueObjects;

/// <summary>
/// Type-safe percentage value object (0–100 scale, stored as 0–1 internally)
/// </summary>
public sealed record Percentage
{
    /// <summary>Decimal fraction (e.g. 0.15 = 15%)</summary>
    public decimal Value { get; init; }

    private Percentage(decimal value) => Value = value;

    /// <summary>Create from decimal fraction (0.15 = 15%)</summary>
    public static Percentage FromFraction(decimal fraction)
    {
        if (fraction < 0 || fraction > 1)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Fraction must be between 0 and 1.");
        return new Percentage(fraction);
    }

    /// <summary>Create from whole-number percent (15 = 15%)</summary>
    public static Percentage FromPercent(decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be between 0 and 100.");
        return new Percentage(percent / 100m);
    }

    public static Percentage Zero => new(0);

    public decimal ApplyTo(decimal amount) => decimal.Round(amount * Value, 2, MidpointRounding.AwayFromZero);
    public override string ToString() => $"{Value * 100:N2}%";
}

/// <summary>
/// Physical address value object with SA-specific fields
/// </summary>
public sealed record Address
{
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string? PostalCode { get; init; }
    public string Country { get; init; } = "South Africa";

    private Address() { }

    public static Address Create(string line1, string city, string province, string country = "South Africa",
        string? line2 = null, string? postalCode = null) =>
        new()
        {
            Line1 = line1,
            Line2 = line2,
            City = city,
            Province = province,
            PostalCode = postalCode,
            Country = country
        };

    public override string ToString()
    {
        var parts = new[] { Line1, Line2, City, Province, PostalCode, Country };
        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
