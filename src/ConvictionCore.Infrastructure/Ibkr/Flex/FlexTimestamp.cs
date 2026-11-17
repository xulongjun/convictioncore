using System.Globalization;

namespace ConvictionCore.Infrastructure.Ibkr.Flex;

/// <summary>
/// Flex timestamps are <c>yyyyMMdd;HHmmss</c> without a zone and mean US Eastern time.
/// </summary>
internal static class FlexTimestamp
{
    public const string TimestampFormat = "yyyyMMdd;HHmmss";
    public const string DateFormat = "yyyyMMdd";

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateTimeOffset ToUtc(string ibkrEasternTimestamp)
    {
        if (!TryToUtc(ibkrEasternTimestamp, out var utc))
        {
            throw new FormatException($"'{ibkrEasternTimestamp}' is not a Flex timestamp ({TimestampFormat}).");
        }

        return utc;
    }

    public static bool TryToUtc(string? ibkrEasternTimestamp, out DateTimeOffset utc)
    {
        utc = default;
        if (!DateTime.TryParseExact(
                ibkrEasternTimestamp,
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var local))
        {
            return false;
        }

        // GetUtcOffset never throws: ambiguous fall-back times resolve to standard time.
        var offset = Eastern.GetUtcOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));
        utc = new DateTimeOffset(local, offset).ToUniversalTime();
        return true;
    }

    public static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static string FormatDate(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);
}
