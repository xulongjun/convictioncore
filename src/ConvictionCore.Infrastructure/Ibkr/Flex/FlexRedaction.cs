using System.Text.RegularExpressions;

namespace ConvictionCore.Infrastructure.Ibkr.Flex;

/// <summary>The Flex token travels as the <c>t=</c> query parameter; never let it reach logs, errors or the DB.</summary>
internal static partial class FlexRedaction
{
    public const string Mask = "***";

    public static string Redact(string text, string? token = null)
    {
        var redacted = TokenParameter().Replace(text, "$1" + Mask);
        if (!string.IsNullOrEmpty(token))
        {
            redacted = redacted.Replace(token, Mask, StringComparison.Ordinal);
        }

        return redacted;
    }

    public static string Redact(Uri uri, string? token = null) => Redact(uri.ToString(), token);

    [GeneratedRegex(@"([?&]t=)[^&\s""'<>]*", RegexOptions.CultureInvariant)]
    private static partial Regex TokenParameter();
}
