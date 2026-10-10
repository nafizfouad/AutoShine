namespace AutoShine.Service.Common;

/// <summary>
/// PostgreSQL "timestamp with time zone" columns only accept UTC DateTimes, so every
/// value is normalised before it reaches EF Core.
/// </summary>
public static class DateTimeUtil
{
    /// <summary>
    /// Date-only values (template ranges, leave days) are stored as UTC midnight of that
    /// calendar date, e.g. "2026-10-01" → 2026-10-01T00:00:00Z.
    /// </summary>
    public static DateTime AsUtcDate(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    public static DateTime AsUtcDate(DateOnly value) =>
        DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    /// <summary>Instants without zone information are treated as UTC.</summary>
    public static DateTime AsUtcInstant(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
