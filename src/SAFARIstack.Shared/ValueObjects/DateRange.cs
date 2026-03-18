namespace SAFARIstack.Shared.ValueObjects;

/// <summary>
/// Canonical DateRange value object — single source of truth used across all modules.
/// Replaces duplicate definitions in Analytics, Channels, and Events modules.
/// </summary>
public sealed record DateRange
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }

    public DateRange(DateTime start, DateTime end)
    {
        if (end < start)
            throw new ArgumentException("End date must be on or after start date.");

        Start = start;
        End = end;
    }

    public int Nights => (End.Date - Start.Date).Days;
    public int Days => Nights + 1;
    public bool Contains(DateTime date) => date.Date >= Start.Date && date.Date <= End.Date;
    public bool Overlaps(DateRange other) => Start < other.End && End > other.Start;

    public IEnumerable<DateTime> EachDay()
    {
        for (var d = Start.Date; d <= End.Date; d = d.AddDays(1))
            yield return d;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} → {End:yyyy-MM-dd} ({Nights}N)";
}
