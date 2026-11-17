using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ConvictionCore.Tests.Shared;

/// <summary>
/// Anonymized fixtures under tests/fixtures/ibkr and the variants the tests derive from them.
/// Variants are built in memory so the committed fixtures stay free of injected test values.
/// </summary>
public static partial class FlexFixtures
{
    public const string AccountId = "U0000001";
    public const string ActivityV3 = "ibkr-activity-v3.anon.xml";
    public const string Synthetic = "ibkr-activity-synthetic.xml";

    public static readonly DateOnly ActivityV3From = new(2026, 1, 1);
    public static readonly DateOnly ActivityV3To = new(2026, 11, 16);
    public static readonly DateOnly SyntheticFrom = new(2026, 3, 1);
    public static readonly DateOnly SyntheticTo = new(2026, 11, 16);

    private static readonly Lazy<string> Root = new(FindRoot);

    public static byte[] Read(string name) => File.ReadAllBytes(Path.Combine(Root.Value, name));

    public static string ReadText(string name) => NormalizeNewlines(File.ReadAllText(Path.Combine(Root.Value, name)));

    public static byte[] Bytes(string xml) => Encoding.UTF8.GetBytes(xml);

    public static string Text(byte[] payload) => NormalizeNewlines(Encoding.UTF8.GetString(payload));

    /// <summary>Fixture edits key off LF breaks; normalize so checks work with CRLF working copies.</summary>
    public static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    /// <summary>Changes one attribute on the Trade[EXECUTION] row with the given transactionID.</summary>
    public static byte[] WithExecutionAttribute(byte[] payload, long transactionId, string attribute, string value)
    {
        var lines = Text(payload).Split('\n');
        var marker = $"transactionID=\"{transactionId.ToString(CultureInfo.InvariantCulture)}\"";
        var index = Array.FindIndex(lines, line => line.StartsWith("<Trade ", StringComparison.Ordinal)
            && line.Contains(marker, StringComparison.Ordinal)
            && line.Contains("levelOfDetail=\"EXECUTION\"", StringComparison.Ordinal));
        if (index < 0)
        {
            throw new InvalidOperationException($"No execution {transactionId} in fixture.");
        }

        lines[index] = Regex.Replace(lines[index], $"(\\s{attribute}=\")[^\"]*(\")", $"${{1}}{value}${{2}}");
        return Bytes(string.Join('\n', lines));
    }

    /// <summary>Keeps the first <paramref name="executions"/> Trade rows (and their Lots) only.</summary>
    public static byte[] WithFirstExecutions(byte[] payload, int executions)
    {
        var kept = new List<string>();
        var seen = 0;
        foreach (var line in Text(payload).Split('\n'))
        {
            if (line.StartsWith("<Trade ", StringComparison.Ordinal))
            {
                seen++;
            }

            var isTradeRow = line.StartsWith("<Trade ", StringComparison.Ordinal) || line.StartsWith("<Lot ", StringComparison.Ordinal);
            if (!isTradeRow || seen <= executions)
            {
                kept.Add(line);
            }
        }

        return Bytes(string.Join('\n', kept));
    }

    public static byte[] WithStatementType(byte[] payload, string type) =>
        Bytes(Text(payload).Replace("type=\"AF\"", $"type=\"{type}\"", StringComparison.Ordinal));

    public static byte[] WithAccountId(byte[] payload, string accountId) =>
        Bytes(Text(payload).Replace($"accountId=\"{AccountId}\"", $"accountId=\"{accountId}\"", StringComparison.Ordinal));

    public static byte[] WithWindow(byte[] payload, DateOnly from, DateOnly to)
    {
        var text = StatementWindow().Replace(
            Text(payload),
            $"fromDate=\"{from:yyyyMMdd}\" toDate=\"{to:yyyyMMdd}\"",
            1);
        return Bytes(text);
    }

    public static byte[] WithAccountInformationAttributes(byte[] payload, string attributes) =>
        Bytes(Text(payload).Replace("<AccountInformation ", $"<AccountInformation {attributes} ", StringComparison.Ordinal));

    /// <summary>Inserts a row immediately after the <c>&lt;Trades&gt;</c> opener (for test variants).</summary>
    public static string InsertAfterTradesOpen(string text, string line) =>
        TradesSectionOpen().Replace(NormalizeNewlines(text), $"<Trades>\n{line}\n", 1);

    /// <summary>Puts a CLOSED_LOT row first in Trades, so it has no parent Trade.</summary>
    public static byte[] WithOrphanLot(byte[] payload)
    {
        var text = Text(payload);
        var lot = text.Split('\n').First(line => line.StartsWith("<Lot ", StringComparison.Ordinal));
        if (!TradesSectionOpen().IsMatch(text))
        {
            throw new InvalidOperationException("Trades section opener not found in fixture.");
        }

        return Bytes(TradesSectionOpen().Replace(text, $"<Trades>\n{lot}\n", 1));
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ConvictionCore.slnx")))
            {
                return Path.Combine(directory.FullName, "tests", "fixtures", "ibkr");
            }
        }

        throw new InvalidOperationException("Repository root (ConvictionCore.slnx) not found.");
    }

    [GeneratedRegex("fromDate=\"\\d{8}\" toDate=\"\\d{8}\"")]
    private static partial Regex StatementWindow();

    [GeneratedRegex("<Trades>\\r?\\n")]
    private static partial Regex TradesSectionOpen();
}
