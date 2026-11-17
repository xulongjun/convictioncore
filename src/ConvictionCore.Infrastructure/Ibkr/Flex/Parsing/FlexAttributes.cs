using System.Globalization;
using System.Xml.Linq;

namespace ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;

/// <summary>Typed attribute access; empty strings read as null.</summary>
internal static class FlexAttributes
{
    public static string? Text(this XElement element, string name)
    {
        var value = element.Attribute(name)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static long? Long(this XElement element, string name)
    {
        var text = element.Text(name);
        if (text is null)
        {
            return null;
        }

        return long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw Invalid(element, name, text);
    }

    public static long RequiredLong(this XElement element, string name) =>
        element.Long(name) ?? throw new FlexParseException($"{element.Name.LocalName}: required attribute '{name}' is empty.");

    public static string RequiredText(this XElement element, string name) =>
        element.Text(name) ?? throw new FlexParseException($"{element.Name.LocalName}: required attribute '{name}' is empty.");

    public static decimal? Decimal(this XElement element, string name)
    {
        var text = element.Text(name);
        if (text is null)
        {
            return null;
        }

        return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw Invalid(element, name, text);
    }

    public static DateOnly? Date(this XElement element, string name)
    {
        var text = element.Text(name);
        if (text is null)
        {
            return null;
        }

        return FlexTimestamp.TryParseDate(text, out var date) ? date : throw Invalid(element, name, text);
    }

    public static DateOnly RequiredDate(this XElement element, string name) =>
        element.Date(name) ?? throw new FlexParseException($"{element.Name.LocalName}: required attribute '{name}' is empty.");

    /// <summary>A Flex Eastern-time timestamp as (UTC instant, original string).</summary>
    public static (DateTimeOffset? Utc, string? IbkrEt) Timestamp(this XElement element, string name)
    {
        var text = element.Text(name);
        if (text is null)
        {
            return (null, null);
        }

        return FlexTimestamp.TryToUtc(text, out var utc) ? (utc, text) : throw Invalid(element, name, text);
    }

    private static FlexParseException Invalid(XElement element, string name, string text) =>
        new($"{element.Name.LocalName}: attribute '{name}' has invalid value '{text}'.");
}
