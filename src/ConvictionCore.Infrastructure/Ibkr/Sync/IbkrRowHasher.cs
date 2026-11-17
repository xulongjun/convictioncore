using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ConvictionCore.Infrastructure.Persistence.Trading.Ibkr;

namespace ConvictionCore.Infrastructure.Ibkr.Sync;

/// <summary>
/// sha256 over the parsed, persisted fields of a fact row: only properties declared on the
/// concrete row type, ordered by name, with invariant, trailing-zero-free numbers. Unrelated
/// XML attributes therefore never change the hash.
/// </summary>
internal static class IbkrRowHasher
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();

    public static string Compute(IbkrFactRow row)
    {
        var properties = Properties.GetOrAdd(
            row.GetType(),
            type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ToArray());

        var canonical = new StringBuilder();
        foreach (var property in properties)
        {
            canonical.Append(property.Name).Append('=').Append(Format(property.GetValue(row))).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    public static string Format(object? value) => value switch
    {
        null => "\u2400",
        decimal number => FormatDecimal(number),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset instant => instant.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string FormatDecimal(decimal value)
    {
        var text = value.ToString("F28", CultureInfo.InvariantCulture);
        if (text.Contains('.', StringComparison.Ordinal))
        {
            text = text.TrimEnd('0').TrimEnd('.');
        }

        return text is "-0" or "" ? "0" : text;
    }
}
