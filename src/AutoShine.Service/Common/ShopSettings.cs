namespace AutoShine.Service.Common;

/// <summary>
/// Shop-wide settings. Schedule templates hold wall-clock times (e.g. 09:00) in the
/// shop's time zone, while bookings are stored as UTC instants.
/// </summary>
public class ShopSettings
{
    public TimeZoneInfo TimeZone { get; }

    public ShopSettings(TimeZoneInfo? timeZone = null) => TimeZone = timeZone ?? TimeZoneInfo.Utc;

    /// <summary>Accepts an IANA ("Asia/Dhaka") or Windows id; empty falls back to the server's local zone.</summary>
    public static ShopSettings FromTimeZoneId(string? timeZoneId) =>
        new(string.IsNullOrWhiteSpace(timeZoneId)
            ? TimeZoneInfo.Local
            : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));

    /// <summary>Converts a shop-local date + time of day to UTC. Null when the time is skipped by a DST change.</summary>
    public DateTime? ToUtc(DateOnly date, TimeSpan timeOfDay)
    {
        var local = date.ToDateTime(TimeOnly.MinValue).Add(timeOfDay); // Kind = Unspecified
        if (TimeZone.IsInvalidTime(local)) return null;
        return TimeZoneInfo.ConvertTimeToUtc(local, TimeZone);
    }

    public DateOnly ToShopDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTimeUtil.AsUtcInstant(utc), TimeZone));
}
