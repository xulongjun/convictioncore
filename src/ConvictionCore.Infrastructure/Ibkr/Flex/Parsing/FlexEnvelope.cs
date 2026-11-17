using System.Xml;

namespace ConvictionCore.Infrastructure.Ibkr.Flex.Parsing;

/// <summary>Header attributes of a Flex statement, read without loading the whole document.</summary>
internal sealed record FlexEnvelope(
    string? RootName,
    string? Type,
    int StatementCount,
    string? AccountId,
    string? FromDate,
    string? ToDate,
    string? WhenGenerated)
{
    public const string ActivityType = "AF";

    public static FlexEnvelope Read(byte[] payload)
    {
        string? rootName = null;
        string? type = null;
        string? accountId = null;
        string? fromDate = null;
        string? toDate = null;
        string? whenGenerated = null;
        var statements = 0;
        try
        {
            using var stream = new MemoryStream(payload, writable: false);
            using var reader = XmlReader.Create(stream, FlexStatementResponse.ReaderSettings);
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (rootName is null)
                {
                    rootName = reader.LocalName;
                    type = reader.GetAttribute("type");
                }
                else if (reader.LocalName == "FlexStatement")
                {
                    statements++;
                    if (statements == 1)
                    {
                        accountId = reader.GetAttribute("accountId");
                        fromDate = reader.GetAttribute("fromDate");
                        toDate = reader.GetAttribute("toDate");
                        whenGenerated = reader.GetAttribute("whenGenerated");
                    }

                    reader.Skip();
                }
            }
        }
        catch (XmlException exception)
        {
            throw new FlexParseException($"Statement is not well-formed XML: {exception.Message}");
        }

        return new FlexEnvelope(rootName, type, statements, accountId, fromDate, toDate, whenGenerated);
    }

    /// <returns>Every mismatch against the expected account and window; empty when valid.</returns>
    public IReadOnlyList<string> Validate(string expectedAccountId, FlexDateWindow window)
    {
        var problems = new List<string>();
        if (RootName != FlexStatementResponse.StatementRootName)
        {
            problems.Add($"root element is '{RootName}', expected '{FlexStatementResponse.StatementRootName}'");
        }

        if (Type != ActivityType)
        {
            problems.Add($"type is '{Type}', expected '{ActivityType}'");
        }

        if (StatementCount != 1)
        {
            problems.Add($"{StatementCount} FlexStatement elements, expected exactly 1");
        }

        if (!string.Equals(AccountId, expectedAccountId, StringComparison.Ordinal))
        {
            problems.Add("accountId does not match the registered IBKR account");
        }

        var expectedFrom = FlexTimestamp.FormatDate(window.From);
        var expectedTo = FlexTimestamp.FormatDate(window.To);
        if (FromDate != expectedFrom || ToDate != expectedTo)
        {
            problems.Add($"statement window is {FromDate}..{ToDate}, requested {expectedFrom}..{expectedTo}");
        }

        return problems;
    }
}
